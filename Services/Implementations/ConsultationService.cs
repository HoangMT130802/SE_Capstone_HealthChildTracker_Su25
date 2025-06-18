using AutoMapper;
using BusinessLogic.DTOs.ConsultationRequest;
using BusinessLogic.DTOs.ConsultationResponse;
using BusinessLogic.DTOs.Doctor;
using BusinessLogic.Services.Interfaces;
using DataAccess.Entities;
using DataAccess.UnitOfWork;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Implementations
{
    public class ConsultationService : IConsultationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<ConsultationService> _logger;
        private const int REQUEST_EXPIRY_HOURS = 24;

        public ConsultationService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<ConsultationService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<ConsultationRequestDTO>> GetAllConsulationRequest()
        {
            try
            {
                var requestRepository = _unitOfWork.GetRepository<ConsultationRequest>();
                var requests = await requestRepository.GetAllAsync();
                return _mapper.Map<IEnumerable<ConsultationRequestDTO>>(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách yêu cầu tham vấn");
                throw;
            }
        }
        public async Task<ConsultationRequestDTO> CreateRequestAsync(int userId, CreateConsultationRequestDTO request)
        {
            try
            {
                // Kiểm tra quyền tư vấn từ membership
                var userMembershipRepo = _unitOfWork.GetRepository<UserMembership>();
                var activeMembership = await userMembershipRepo.GetAsync(
                    um => um.UserId == userId &&
                          um.Status == "Active" &&
                          um.EndDate > DateTime.UtcNow,
                    includeProperties: "Membership"
                );

                if (activeMembership == null)
                {
                    throw new InvalidOperationException("Bạn cần có gói membership active để sử dụng dịch vụ tư vấn");
                }

                if (!activeMembership.Membership.CanAccessConsultation)
                {
                    throw new InvalidOperationException("Gói membership của bạn không có quyền sử dụng dịch vụ tư vấn");
                }

                if (activeMembership.RemainingConsultations <= 0)
                {
                    throw new InvalidOperationException("Bạn đã hết lượt tư vấn trong gói membership hiện tại");
                }

                // Kiểm tra child có thuộc về user không
                var childRepo = _unitOfWork.GetRepository<Child>();
                var child = await childRepo.GetAsync(c => c.ChildId == request.ChildId && c.UserId == userId);
                if (child == null)
                {
                    throw new InvalidOperationException("Không tìm thấy thông tin trẻ hoặc trẻ không thuộc về bạn");
                }

                var consultationRequest = _mapper.Map<ConsultationRequest>(request);
                consultationRequest.UserId = userId;
                consultationRequest.Status = "Pending";
                consultationRequest.CreatedAt = DateTime.UtcNow;
                consultationRequest.LastActivityAt = DateTime.UtcNow;

                var repo = _unitOfWork.GetRepository<ConsultationRequest>();
                await repo.AddAsync(consultationRequest);

                // Giảm số lượt tư vấn còn lại
                activeMembership.RemainingConsultations--;
                userMembershipRepo.Update(activeMembership);

                await _unitOfWork.SaveChangesAsync();

                // Thêm câu hỏi đầu tiên
                var responseRepo = _unitOfWork.GetRepository<ConsultationResponse>();
                await responseRepo.AddAsync(new ConsultationResponse
                {
                    RequestId = consultationRequest.RequestId,
                    Response = request.Description,
                    DoctorId = null,
                    ParentResponseId = null,
                    IsQuestion = true,
                    IsFromUser = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Attachments = ""
                });

                await _unitOfWork.SaveChangesAsync();

                // Tự động phân công bác sĩ
                await AutoAssignDoctorAsync(consultationRequest.RequestId);

                return await GetRequestByIdAsync(consultationRequest.RequestId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo yêu cầu tư vấn");
                throw;
            }
        }

        private async Task AutoAssignDoctorAsync(int requestId)
        {
            var doctorWorkload = await GetDoctorWorkloadAsync();
            if (!doctorWorkload.Any()) return;

            // Chọn bác sĩ có ít việc nhất
            var doctorId = doctorWorkload.OrderBy(x => x.Value).First().Key;
            await AssignDoctorAsync(requestId, doctorId);
        }

        public async Task<IEnumerable<ConsultationResponseDTO>> GetDoctorResponsesAsync(int doctorId)
        {
            try
            {
                var responseRepo = _unitOfWork.GetRepository<ConsultationResponse>();
                var responses = await responseRepo.FindAsync(
                    r => r.DoctorId == doctorId,
                    includeProperties: "Request,Request.User,Request.Child"
                );

                // Sắp xếp theo thời gian mới nhất
                var sortedResponses = responses.OrderByDescending(r => r.CreatedAt);

                return _mapper.Map<IEnumerable<ConsultationResponseDTO>>(sortedResponses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy danh sách phản hồi của bác sĩ {doctorId}");
                throw;
            }
        }
        public async Task<ConsultationResponseDTO> CreateResponseAsync(int requestId, int doctorId, CreateConsultationResponseDTO response)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Kiểm tra request có tồn tại và trạng thái hợp lệ
                var requestRepo = _unitOfWork.GetRepository<ConsultationRequest>();
                var request = await requestRepo.GetAsync(r => r.RequestId == requestId);

                if (request == null)
                    throw new KeyNotFoundException("Không tìm thấy yêu cầu tư vấn");

                if (request.Status == "Closed" || request.Status == "Expired")
                    throw new InvalidOperationException("Yêu cầu tư vấn đã đóng hoặc hết hạn");

                if (request.AssignedDoctorId != doctorId)
                    throw new InvalidOperationException("Bạn không được phân công cho yêu cầu tư vấn này");

                var consultationResponseRepo = _unitOfWork.GetRepository<ConsultationResponse>();

                // Kiểm tra câu hỏi tồn tại nếu có ParentResponseId
                if (response.ParentResponseId.HasValue)
                {
                    var question = await consultationResponseRepo.GetAsync(r =>
                        r.ResponseId == response.ParentResponseId.Value &&
                        r.RequestId == requestId &&
                        r.IsQuestion);

                    if (question == null)
                        throw new KeyNotFoundException("Không tìm thấy câu hỏi cần trả lời");
                }

                var consultationResponse = _mapper.Map<ConsultationResponse>(response);
                consultationResponse.RequestId = requestId;
                consultationResponse.DoctorId = doctorId;
                consultationResponse.IsFromUser = false;
                consultationResponse.IsQuestion = false;
                consultationResponse.CreatedAt = DateTime.UtcNow;
                consultationResponse.UpdatedAt = DateTime.UtcNow;

                await consultationResponseRepo.AddAsync(consultationResponse);

                // Cập nhật trạng thái và thời gian hoạt động của request
                request.Status = "InProgress";
                request.LastActivityAt = DateTime.UtcNow;
                requestRepo.Update(request);

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                return _mapper.Map<ConsultationResponseDTO>(consultationResponse);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi tạo phản hồi tư vấn");
                throw;
            }
        }

        public async Task<ConsultationResponseDTO> AddResponseAsync(
            int requestId,
            int userId,
            AskQuestionDTO questionDto,
            int? parentResponseId = null,
            bool isFromDoctor = false)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var requestRepo = _unitOfWork.GetRepository<ConsultationRequest>();
                var request = await requestRepo.GetAsync(r => r.RequestId == requestId);

                if (request == null)
                    throw new KeyNotFoundException("Không tìm thấy yêu cầu tư vấn");

                if (request.Status == "Closed" || request.Status == "Expired")
                    throw new InvalidOperationException("Yêu cầu tư vấn đã đóng hoặc hết hạn");

                // Kiểm tra quyền
                if (isFromDoctor && request.AssignedDoctorId != userId)
                    throw new InvalidOperationException("Bạn không được phân công cho yêu cầu tư vấn này");
                else if (!isFromDoctor && request.UserId != userId)
                    throw new InvalidOperationException("Bạn không có quyền thêm câu hỏi cho yêu cầu này");

                var responseRepo = _unitOfWork.GetRepository<ConsultationResponse>();

                // Kiểm tra parentResponse nếu có
                if (parentResponseId.HasValue)
                {
                    var parentResponse = await responseRepo.GetAsync(r => 
                        r.ResponseId == parentResponseId.Value && 
                        r.RequestId == requestId);

                    if (parentResponse == null)
                        throw new KeyNotFoundException("Không tìm thấy phản hồi gốc");

                    // Nếu là bác sĩ trả lời, parent phải là câu hỏi
                    if (isFromDoctor && !parentResponse.IsQuestion)
                        throw new InvalidOperationException("Bác sĩ chỉ có thể trả lời cho câu hỏi");

                    // Nếu là user hỏi tiếp, parent phải là câu trả lời
                    if (!isFromDoctor && parentResponse.IsQuestion)
                        throw new InvalidOperationException("Người dùng chỉ có thể hỏi tiếp theo câu trả lời");
                }
                else if (isFromDoctor)
                {
                    _logger.LogInformation($"Tìm câu hỏi chưa được trả lời cho request {requestId}");
                    
                    // Lấy tất cả responses
                    var allResponses = await responseRepo.FindAsync(r => r.RequestId == requestId);
                    
                    // Lọc ra các câu hỏi từ user
                    var questions = allResponses
                        .Where(r => r.IsQuestion && r.IsFromUser)
                        .OrderBy(r => r.CreatedAt)
                        .ToList();
                    
                    // Lọc ra các câu trả lời từ bác sĩ (không tính thông báo hệ thống)
                    var doctorAnswers = allResponses
                        .Where(r => !r.IsQuestion && !r.IsFromUser && !string.IsNullOrEmpty(r.DoctorId.ToString()))
                        .ToList();

                    _logger.LogInformation($"Tổng số câu hỏi từ user: {questions.Count}");
                    _logger.LogInformation($"Tổng số câu trả lời từ bác sĩ: {doctorAnswers.Count}");

                    // Log chi tiết các câu hỏi
                    foreach (var q in questions)
                    {
                        _logger.LogInformation($"Câu hỏi ID {q.ResponseId}: {q.Response}, Created: {q.CreatedAt}");
                    }

                    // Log chi tiết các câu trả lời
                    foreach (var a in doctorAnswers)
                    {
                        _logger.LogInformation($"Câu trả lời ID {a.ResponseId} cho câu hỏi {a.ParentResponseId}: {a.Response}");
                    }

                    // Tìm câu hỏi chưa được trả lời
                    var answeredQuestionIds = doctorAnswers.Select(a => a.ParentResponseId).ToList();
                    var unansweredQuestions = questions
                        .Where(q => !answeredQuestionIds.Contains(q.ResponseId))
                        .OrderBy(q => q.CreatedAt)
                        .ToList();

                    _logger.LogInformation($"Số câu hỏi chưa được trả lời: {unansweredQuestions.Count}");
                    foreach (var q in unansweredQuestions)
                    {
                        _logger.LogInformation($"Câu hỏi chưa trả lời ID {q.ResponseId}: {q.Response}, Created: {q.CreatedAt}");
                    }

                    var unansweredQuestion = unansweredQuestions.FirstOrDefault();
                    if (unansweredQuestion == null)
                    {
                        _logger.LogWarning($"Không tìm thấy câu hỏi chưa được trả lời cho request {requestId}");
                        throw new InvalidOperationException("Không tìm thấy câu hỏi chưa được trả lời");
                    }

                    _logger.LogInformation($"Đã tìm thấy câu hỏi chưa trả lời: ID {unansweredQuestion.ResponseId}");
                    parentResponseId = unansweredQuestion.ResponseId;
                }

                // Tạo response mới
                var response = new ConsultationResponse
                {
                    RequestId = requestId,
                    Response = isFromDoctor ? questionDto.Question : questionDto.Question,
                    DoctorId = isFromDoctor ? userId : null,
                    ParentResponseId = parentResponseId,
                    IsQuestion = !isFromDoctor,
                    IsFromUser = !isFromDoctor,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Attachments = questionDto.Attachments ?? ""
                };

                await responseRepo.AddAsync(response);

                // Cập nhật trạng thái request
                request.Status = "InProgress";
                request.LastActivityAt = DateTime.UtcNow;
                requestRepo.Update(request);

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                return _mapper.Map<ConsultationResponseDTO>(response);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi thêm phản hồi cho yêu cầu tư vấn {RequestId}", requestId);
                throw;
            }
        }

        public async Task<ConsultationRequestDTO> GetRequestByIdAsync(int requestId, int userId)
        {
            try
            {
                var requestRepo = _unitOfWork.GetRepository<ConsultationRequest>();
                var request = await requestRepo.GetAsync(
                    r => r.RequestId == requestId && (r.UserId == userId || r.AssignedDoctorId == userId),
                    includeProperties: "User,Child,AssignedDoctor,ConsultationResponses"
                );

                if (request == null)
                    throw new KeyNotFoundException("Không tìm thấy yêu cầu tư vấn");

                if (request.UserId != userId && request.AssignedDoctorId != userId)
                    throw new UnauthorizedAccessException("Bạn không có quyền xem yêu cầu tư vấn này");

                return _mapper.Map<ConsultationRequestDTO>(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thông tin yêu cầu tư vấn {RequestId}", requestId);
                throw;
            }
        }

        public async Task<IEnumerable<ConsultationRequestDTO>> GetUserRequestsAsync(int userId)
        {
            try
            {
                var repo = _unitOfWork.GetRepository<ConsultationRequest>();
                var requests = await repo.FindAsync(
                    r => r.UserId == userId,
                    includeProperties: "User,Child,AssignedDoctor,ConsultationResponses,ConsultationResponses.Doctor"
                );

                return _mapper.Map<IEnumerable<ConsultationRequestDTO>>(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy danh sách yêu cầu tư vấn của user {userId}");
                throw;
            }
        }

        public async Task<IEnumerable<ConsultationRequestDTO>> GetDoctorRequestsAsync(int doctorId)
        {
            try
            {
                var repo = _unitOfWork.GetRepository<ConsultationRequest>();
                var requests = await repo.FindAsync(
                    r => r.AssignedDoctorId == doctorId,
                    includeProperties: "User,Child,AssignedDoctor,ConsultationResponses,ConsultationResponses.Doctor"
                );

                return _mapper.Map<IEnumerable<ConsultationRequestDTO>>(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy danh sách yêu cầu tư vấn của bác sĩ {doctorId}");
                throw;
            }
        }

        public async Task<ConsultationRequestDTO> UpdateRequestStatusAsync(
            int requestId,
            int userId,
            string action,
            string reason = null,
            bool? isSatisfied = null)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var requestRepo = _unitOfWork.GetRepository<ConsultationRequest>();
                var request = await requestRepo.GetAsync(r => r.RequestId == requestId);

                if (request == null)
                    throw new KeyNotFoundException("Không tìm thấy yêu cầu tư vấn");

                if (request.Status == "Closed" || request.Status == "Expired")
                    throw new InvalidOperationException("Yêu cầu tư vấn đã đóng hoặc hết hạn");

                switch (action.ToLower())
                {
                    case "complete":
                        if (request.UserId != userId)
                            throw new UnauthorizedAccessException("Chỉ người tạo yêu cầu mới có thể đánh dấu hoàn thành");

                        var responseRepo = _unitOfWork.GetRepository<ConsultationResponse>();
                        var hasResponses = await responseRepo.AnyAsync(r => r.RequestId == requestId && !r.IsQuestion);
                        
                        if (!hasResponses)
                            throw new InvalidOperationException("Không thể hoàn thành yêu cầu khi chưa có câu trả lời từ bác sĩ");

                        request.Status = "Completed";
                        request.IsSatisfied = isSatisfied ?? false;
                        request.ClosedReason = isSatisfied == true ? 
                            "Người dùng hài lòng với tư vấn" : 
                            "Người dùng không hài lòng với tư vấn";
                        request.ClosedAt = DateTime.UtcNow;
                        break;

                    case "close":
                        request.Status = "Closed";
                        request.ClosedReason = reason;
                        request.ClosedAt = DateTime.UtcNow;
                        break;

                    default:
                        throw new ArgumentException("Hành động không hợp lệ");
                }

                request.LastActivityAt = DateTime.UtcNow;
                requestRepo.Update(request);
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                return await GetRequestByIdAsync(requestId, userId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi cập nhật trạng thái yêu cầu tư vấn {RequestId}", requestId);
                throw;
            }
        }

        public async Task<ConsultationResponseDTO> UpdateResponseContentAsync(int responseId, string newContent)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var responseRepo = _unitOfWork.GetRepository<ConsultationResponse>();
                var response = await responseRepo.GetAsync(
                    r => r.ResponseId == responseId,
                    includeProperties: "Request,Doctor"
                );

                if (response == null)
                    throw new KeyNotFoundException($"Không tìm thấy phản hồi ID {responseId}");

                if (response.Request.Status == "Closed" || response.Request.Status == "Expired")
                    throw new InvalidOperationException("Không thể cập nhật phản hồi cho yêu cầu đã đóng hoặc hết hạn");

                response.Response = newContent;
                response.UpdatedAt = DateTime.UtcNow;

                var requestRepo = _unitOfWork.GetRepository<ConsultationRequest>();
                var request = await requestRepo.GetAsync(r => r.RequestId == response.RequestId);
                request.LastActivityAt = DateTime.UtcNow;

                responseRepo.Update(response);
                requestRepo.Update(request);
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                return _mapper.Map<ConsultationResponseDTO>(response);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi cập nhật nội dung phản hồi {ResponseId}", responseId);
                throw;
            }
        }

        public async Task<ConsultationRequestDTO> AssignDoctorAsync(int requestId, int doctorId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Kiểm tra bác sĩ có tồn tại
                var doctorRepo = _unitOfWork.GetRepository<User>();
                var doctor = await doctorRepo.GetAsync(d => d.UserId == doctorId && d.Role == "Doctor");

                if (doctor == null)
                    throw new InvalidOperationException($"Không tìm thấy bác sĩ ID {doctorId}");

                // Kiểm tra và cập nhật request
                var requestRepo = _unitOfWork.GetRepository<ConsultationRequest>();
                var request = await requestRepo.GetAsync(r => r.RequestId == requestId);

                if (request == null)
                    throw new KeyNotFoundException($"Không tìm thấy yêu cầu tư vấn ID {requestId}");

                if (request.Status != "Pending")
                    throw new InvalidOperationException("Yêu cầu tư vấn không ở trạng thái chờ xử lý");

                request.Status = "Assigned";
                request.AssignedDoctorId = doctorId;
                request.LastActivityAt = DateTime.UtcNow;

                requestRepo.Update(request);

                // Tạo response thông báo phân công
                var responseRepo = _unitOfWork.GetRepository<ConsultationResponse>();
                await responseRepo.AddAsync(new ConsultationResponse
                {
                    RequestId = requestId,
                    DoctorId = doctorId,
                    Response = "Bác sĩ đã được phân công xử lý yêu cầu này",
                    IsFromUser = false,
                    IsQuestion = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Attachments = ""
                });

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                return await GetRequestByIdAsync(requestId, request.UserId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Lỗi khi phân công bác sĩ cho yêu cầu {requestId}");
                throw;
            }
        }

        public async Task<Dictionary<int, int>> GetDoctorWorkloadAsync()
        {
            try
            {
                var doctorRepo = _unitOfWork.GetRepository<User>();
                var requestRepo = _unitOfWork.GetRepository<ConsultationRequest>();

                // Lấy danh sách bác sĩ
                var doctors = await doctorRepo.FindAsync(u => u.Role == "Doctor");
                var workload = new Dictionary<int, int>();

                foreach (var doctor in doctors)
                {
                    // Đếm số lượng request đang xử lý của mỗi bác sĩ
                    var activeRequests = await requestRepo.FindAsync(r =>
                        r.AssignedDoctorId == doctor.UserId &&
                        r.Status != "Closed" &&
                        r.Status != "Expired");

                    workload.Add(doctor.UserId, activeRequests.Count());
                }

                return workload;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thông tin khối lượng công việc của bác sĩ");
                throw;
            }
        }

        public async Task<ConsultationRequestDTO> CompleteRequestAsync(int requestId, int userId, bool isSatisfied)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var requestRepo = _unitOfWork.GetRepository<ConsultationRequest>();
                var request = await requestRepo.GetAsync(r => r.RequestId == requestId && r.UserId == userId);

                if (request == null)
                    throw new KeyNotFoundException("Không tìm thấy yêu cầu tư vấn");

                if (request.Status == "Closed" || request.Status == "Expired")
                    throw new InvalidOperationException("Yêu cầu tư vấn đã đóng hoặc hết hạn");

                // Kiểm tra xem có câu trả lời nào chưa
                var responseRepo = _unitOfWork.GetRepository<ConsultationResponse>();
                var hasResponses = await responseRepo.AnyAsync(r => r.RequestId == requestId && !r.IsQuestion);
                
                if (!hasResponses)
                    throw new InvalidOperationException("Không thể hoàn thành yêu cầu khi chưa có câu trả lời từ bác sĩ");

                request.Status = "Completed";
                request.IsSatisfied = isSatisfied;
                request.LastActivityAt = DateTime.UtcNow;
                request.ClosedAt = DateTime.UtcNow;
                request.ClosedReason = isSatisfied ? "Người dùng hài lòng với tư vấn" : "Người dùng không hài lòng với tư vấn";

                requestRepo.Update(request);
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                return _mapper.Map<ConsultationRequestDTO>(request);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi hoàn thành yêu cầu tư vấn {RequestId}", requestId);
                throw;
            }
        }

        public async Task CheckAndUpdateExpiredRequestsAsync()
        {
            try
            {
                var requestRepo = _unitOfWork.GetRepository<ConsultationRequest>();
                var expirationTime = DateTime.UtcNow.AddHours(-24);
                var expiredRequests = await requestRepo.FindAsync(
                    r => r.Status == "Pending" && r.LastActivityAt < expirationTime
                );

                foreach (var request in expiredRequests)
                {
                    request.Status = "Expired";
                    request.LastActivityAt = DateTime.UtcNow;
                }

                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật trạng thái các yêu cầu tư vấn hết hạn");
                throw;
            }
        }

        public async Task<ConsultationResponseDTO> AddDoctorResponseAsync(int requestId, int? questionId, int doctorId, DoctorResponseDTO responseDto)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var requestRepo = _unitOfWork.GetRepository<ConsultationRequest>();
                var request = await requestRepo.GetAsync(r => r.RequestId == requestId);

                if (request == null)
                    throw new KeyNotFoundException("Không tìm thấy yêu cầu tư vấn");

                if (request.Status == "Closed" || request.Status == "Expired")
                    throw new InvalidOperationException("Yêu cầu tư vấn đã đóng hoặc hết hạn");

                if (request.AssignedDoctorId != doctorId)
                    throw new InvalidOperationException("Bạn không được phân công cho yêu cầu tư vấn này");

                var consultationResponseRepo = _unitOfWork.GetRepository<ConsultationResponse>();
                int parentQuestionId;

                if (questionId.HasValue)
                {
                    // Kiểm tra câu hỏi cụ thể
                    var question = await consultationResponseRepo.GetAsync(r => 
                        r.ResponseId == questionId.Value && 
                        r.RequestId == requestId && 
                        r.IsQuestion);

                    if (question == null)
                        throw new KeyNotFoundException("Không tìm thấy câu hỏi cần trả lời");

                    parentQuestionId = questionId.Value;
                }
                else
                {
                    // Tìm câu hỏi gần nhất chưa được trả lời
                    var questions = await consultationResponseRepo.FindAsync(r => 
                        r.RequestId == requestId && 
                        r.IsQuestion && 
                        r.IsFromUser);

                    var answers = await consultationResponseRepo.FindAsync(r => 
                        r.RequestId == requestId && 
                        !r.IsQuestion);

                    var unansweredQuestions = questions
                        .Where(q => !answers.Any(a => a.ParentResponseId == q.ResponseId))
                        .OrderBy(q => q.CreatedAt);

                    var lastQuestion = unansweredQuestions.FirstOrDefault();
                    if (lastQuestion == null)
                        throw new InvalidOperationException("Không tìm thấy câu hỏi chưa được trả lời");

                    parentQuestionId = lastQuestion.ResponseId;
                }

                var response = _mapper.Map<ConsultationResponse>(responseDto);
                response.RequestId = requestId;
                response.DoctorId = doctorId;
                response.ParentResponseId = parentQuestionId;
                response.IsQuestion = false;
                response.IsFromUser = false;
                response.CreatedAt = DateTime.UtcNow;
                response.UpdatedAt = DateTime.UtcNow;

                await consultationResponseRepo.AddAsync(response);

                request.Status = "InProgress";
                request.LastActivityAt = DateTime.UtcNow;
                requestRepo.Update(request);

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                return _mapper.Map<ConsultationResponseDTO>(response);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi tạo phản hồi tư vấn");
                throw;
            }
        }
    }
}