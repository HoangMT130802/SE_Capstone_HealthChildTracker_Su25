using AutoMapper;
using Contracts.DTOs.VaccinationFacilityPaymentAccount;
using Contracts.DTOs.Transaction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Net.payOS;
using Net.payOS.Types;
using Repositories.Entities;
using Repositories.Interfaces;
using Repositories.Models.QueryModels;
using Services.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;
using Transaction = Repositories.Entities.Transaction;

public class VaccinationFacilityPaymentAccountService : IVaccinationFacilityPaymentAccountService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<VaccinationFacilityPaymentAccountService> _logger;
    private readonly IConfiguration _configuration;

    public VaccinationFacilityPaymentAccountService(
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger<VaccinationFacilityPaymentAccountService> logger,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    private async Task ValidateManagerAccess(int accountId)
    {
        var staffRepository = _unitOfWork.GetRepository<FacilityStaff>();
        var staff = await staffRepository.GetAsync(s => s.AccountId == accountId && s.Position == "Manager");
        if (staff == null)
        {
            throw new UnauthorizedAccessException($"User with AccountId {accountId} is not a Manager or does not belong to Facility");
        }
    }

    /// <summary>
    /// Lấy PayOS instance cho facility cụ thể
    /// </summary>
    private async Task<PayOS> GetFacilityPayOSInstanceAsync(int facilityId)
    {
        var paymentAccount = await GetActiveFacilityPaymentAccountAsync(facilityId);
        
        // Log PayOS credentials để debug (chỉ log một phần để bảo mật)
        _logger.LogInformation("Creating PayOS instance for FacilityId {FacilityId} with ClientId: {ClientId}, ApiKey: {ApiKey}, ChecksumKey: {ChecksumKey}", 
            facilityId, 
            paymentAccount.ClientId?.Substring(0, Math.Min(8, paymentAccount.ClientId.Length)) + "...",
            paymentAccount.ApiKey?.Substring(0, Math.Min(8, paymentAccount.ApiKey.Length)) + "...",
            paymentAccount.ChecksumKey?.Substring(0, Math.Min(8, paymentAccount.ChecksumKey.Length)) + "...");
        
        return new PayOS(paymentAccount.ClientId!, paymentAccount.ApiKey!, paymentAccount.ChecksumKey!);
    }

    /// <summary>
    /// Lấy thông tin PayOS account đang active của facility
    /// </summary>
    private async Task<VaccinationFacilityPaymentAccount> GetActiveFacilityPaymentAccountAsync(int facilityId)
    {
        var repository = _unitOfWork.GetRepository<VaccinationFacilityPaymentAccount>();
        var paymentAccount = await repository.GetAsync(pa => pa.FacilityId == facilityId && pa.IsActive == "true");
        
        if (paymentAccount == null)
        {
            _logger.LogError("❌ Facility {FacilityId} chưa cấu hình PayOS account hoặc account không active", facilityId);
            throw new InvalidOperationException($"Facility {facilityId} chưa cấu hình PayOS account hoặc account không active");
        }

        if (string.IsNullOrEmpty(paymentAccount.ClientId) || 
            string.IsNullOrEmpty(paymentAccount.ApiKey) || 
            string.IsNullOrEmpty(paymentAccount.ChecksumKey))
        {
            _logger.LogError("❌ PayOS configuration không đầy đủ cho facility {FacilityId}. ClientId: {HasClientId}, ApiKey: {HasApiKey}, ChecksumKey: {HasChecksumKey}", 
                facilityId, 
                !string.IsNullOrEmpty(paymentAccount.ClientId),
                !string.IsNullOrEmpty(paymentAccount.ApiKey),
                !string.IsNullOrEmpty(paymentAccount.ChecksumKey));
            throw new InvalidOperationException($"PayOS configuration không đầy đủ cho facility {facilityId}");
        }

        // Validate PayOS credentials format
        if (!IsValidPayOSCredentials(paymentAccount))
        {
            _logger.LogError("❌ PayOS credentials không hợp lệ cho facility {FacilityId}", facilityId);
            throw new InvalidOperationException($"PayOS credentials không hợp lệ cho facility {facilityId}");
        }

        return paymentAccount;
    }

    /// <summary>
    /// Validate PayOS credentials format
    /// </summary>
    private bool IsValidPayOSCredentials(VaccinationFacilityPaymentAccount paymentAccount)
    {
        // Basic validation - có thể thêm regex hoặc format checks khác
        if (paymentAccount.ClientId.Length < 10 || 
            paymentAccount.ApiKey.Length < 20 || 
            paymentAccount.ChecksumKey.Length < 20)
        {
            return false;
        }

        // PayOS ClientId thường bắt đầu bằng số hoặc có format đặc biệt
        // ApiKey và ChecksumKey thường là hex string hoặc base64
        return true;
    }

    public async Task<int> CreatePaymentAccountAsync(CreateVaccinationFacilityPaymentAccountDto paymentAccountDto, int accountId)
    {
        try
        {
            await ValidateManagerAccess(accountId);

            var repository = _unitOfWork.GetRepository<VaccinationFacilityPaymentAccount>();
            
            // Nếu tạo account active, deactivate các account khác của facility này
            if (paymentAccountDto.IsActive)
            {
                var existingAccountsResult = await repository.GetAllAsync(
                    filter: pa => pa.FacilityId == paymentAccountDto.FacilityId,
                    orderBy: null,
                    pageIndex: null,
                    pageSize: null
                );

                var activeAccountIds = existingAccountsResult.Data?
                    .Where(pa => pa.IsActive == "true")
                    .Select(pa => pa.Id)
                    .ToList() ?? new List<int>();

                if (activeAccountIds.Any())
                {
                    foreach (var id in activeAccountIds)
                    {
                        var accountToUpdate = await repository.GetAsync(pa => pa.Id == id);
                        if (accountToUpdate != null)
                        {
                            accountToUpdate.IsActive = "false";
                            repository.Update(accountToUpdate);
                        }
                    }
                    await _unitOfWork.SaveChangesAsync();
                }
            }

            var paymentAccount = _mapper.Map<VaccinationFacilityPaymentAccount>(paymentAccountDto);
            await repository.AddAsync(paymentAccount);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"Created payment account with ID {paymentAccount.Id} by AccountId {accountId}");
            return paymentAccount.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating payment account by AccountId {accountId}: {ex.Message}");
            throw;
        }
    }

    public async Task UpdatePaymentAccountAsync(int id, UpdateVaccinationFacilityPaymentAccountDto paymentAccountDto, int accountId)
    {
        try
        {
            await ValidateManagerAccess(accountId);

            var repository = _unitOfWork.GetRepository<VaccinationFacilityPaymentAccount>();
            var paymentAccount = await repository.GetAsync(pa => pa.Id == id);
            if (paymentAccount == null)
                throw new KeyNotFoundException($"Payment account with ID {id} not found");

            if (paymentAccountDto.IsActive && paymentAccount.IsActive != "true")
            {
                var existingAccountsResult = await repository.GetAllAsync(
                    filter: pa => pa.FacilityId == paymentAccount.FacilityId && pa.Id != id,
                    orderBy: null,
                    pageIndex: null,
                    pageSize: null
                );
                var activeAccountIds = existingAccountsResult.Data?
                    .Where(pa => pa.IsActive == "true")
                    .Select(pa => pa.Id)
                    .ToList() ?? new List<int>();

                if (activeAccountIds.Any())
                {
                    foreach (var accountIdToUpdate in activeAccountIds)
                    {
                        var accountToUpdate = await repository.GetAsync(pa => pa.Id == accountIdToUpdate);
                        if (accountToUpdate != null)
                        {
                            accountToUpdate.IsActive = "false";
                            repository.Update(accountToUpdate);
                        }
                    }
                    await _unitOfWork.SaveChangesAsync();
                }
            }

            // Update fields từ DTO
            _mapper.Map(paymentAccountDto, paymentAccount);

            repository.Update(paymentAccount);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"Updated payment account with ID {id} by AccountId {accountId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating payment account with ID {id} by AccountId {accountId}: {ex.Message}");
            throw;
        }
    }

    public async Task DeletePaymentAccountAsync(int id, int accountId)
    {
        try
        {
            await ValidateManagerAccess(accountId);

            var repository = _unitOfWork.GetRepository<VaccinationFacilityPaymentAccount>();
            var paymentAccount = await repository.GetAsync(pa => pa.Id == id);
            if (paymentAccount == null)
                throw new KeyNotFoundException($"Payment account with ID {id} not found");

            // Xóa PayOS account - không cần xóa file

            repository.Delete(paymentAccount);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"Deleted payment account with ID {id} by AccountId {accountId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting payment account with ID {id} by AccountId {accountId}: {ex.Message}");
            throw;
        }
    }

    public async Task<VaccinationFacilityPaymentAccountDto> GetPaymentAccountByIdAsync(int id)
    {
        try
        {
            var repository = _unitOfWork.GetRepository<VaccinationFacilityPaymentAccount>();
            var paymentAccount = await repository.GetAsync(pa => pa.Id == id);
            if (paymentAccount == null)
                throw new KeyNotFoundException($"Payment account with ID {id} not found");

            return _mapper.Map<VaccinationFacilityPaymentAccountDto>(paymentAccount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting payment account with ID {id}: {ex.Message}");
            throw;
        }
    }

    public async Task<QueryResultModel<IEnumerable<VaccinationFacilityPaymentAccountDto>>> GetAllPaymentAccountsAsync(bool? isActive = null, int? pageIndex = null, int? pageSize = null)
    {
        try
        {
            var repository = _unitOfWork.GetRepository<VaccinationFacilityPaymentAccount>();
            var paymentAccountsResult = await repository.GetAllAsync(
                filter: isActive.HasValue ? pa => pa.IsActive == (isActive.Value ? "true" : "false") : null,
                orderBy: null,
                pageIndex: pageIndex,
                pageSize: pageSize
            );
            var dtos = _mapper.Map<IEnumerable<VaccinationFacilityPaymentAccountDto>>(paymentAccountsResult.Data);
            return new QueryResultModel<IEnumerable<VaccinationFacilityPaymentAccountDto>>
            {
                TotalCount = paymentAccountsResult.TotalCount,
                Data = dtos
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting all payment accounts: {ex.Message}");
            throw;
        }
    }

    public async Task<QueryResultModel<IEnumerable<VaccinationFacilityPaymentAccountDto>>> GetPaymentAccountByFacilityIdAsync(int facilityId, bool? isActive = null, int? pageIndex = null, int? pageSize = null)
    {
        try
        {
            var repository = _unitOfWork.GetRepository<VaccinationFacilityPaymentAccount>();
            var paymentAccountsResult = await repository.GetAllAsync(
                filter: pa => pa.FacilityId == facilityId && (!isActive.HasValue || pa.IsActive == (isActive.Value ? "true" : "false")),
                orderBy: null,
                pageIndex: pageIndex,
                pageSize: pageSize
            );
            var dtos = _mapper.Map<IEnumerable<VaccinationFacilityPaymentAccountDto>>(paymentAccountsResult.Data);
            return new QueryResultModel<IEnumerable<VaccinationFacilityPaymentAccountDto>>
            {
                TotalCount = paymentAccountsResult.TotalCount,
                Data = dtos
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting payment accounts for FacilityId {facilityId}: {ex.Message}");
            throw;
        }
    }

    #region Payment Methods

    /// <summary>
    /// Tạo payment link dựa trên AppointmentId - tự động phát hiện loại thanh toán
    /// </summary>
    public async Task<FacilityPaymentResponseDTO> CreateFacilityPaymentAsync(CreateFacilityPaymentDTO request, int accountId)
    {
        using var dbTransaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            _logger.LogInformation("Bắt đầu tạo facility payment cho AppointmentId: {AppointmentId}", request.AppointmentId);

            // Lấy thông tin appointment và validate
            var appointmentInfo = await GetAppointmentInfoAsync(request.AppointmentId);
            
            // Validate quyền truy cập facility
            await ValidateFacilityAccess(accountId, appointmentInfo.FacilityId);

            // Tính toán amount và description dựa trên appointment
            var (amount, description, paymentType, orderId) = await CalculatePaymentFromAppointment(appointmentInfo);

            // Tạo PayOS payment link với config của facility
            var payOS = await GetFacilityPayOSInstanceAsync(appointmentInfo.FacilityId);
            var orderCode = GenerateOrderCode(appointmentInfo.FacilityId, request.AppointmentId, orderId);

            var paymentData = new PaymentData(
                long.Parse(orderCode.Split('_')[0]), // timestamp part
                (int)amount,
                TruncateDescription(description),
                new List<ItemData>(),
                $"{GetFrontendUrl()}/staff/appointments/{request.AppointmentId}/step-3?orderId={orderCode}&status=cancel",
                $"http://localhost:5173/staff/appointments/{request.AppointmentId}/payment-complete",
                null
            );

            _logger.LogInformation("Creating PayOS payment link - OrderCode: {OrderCode}, Amount: {Amount}, Description: {Description}", 
                orderCode, amount, description);

            CreatePaymentResult createPayment;
            try
            {
                createPayment = await payOS.createPaymentLink(paymentData);
                _logger.LogInformation("✅ PayOS payment link created successfully: {CheckoutUrl}", createPayment.checkoutUrl);
            }
            catch (Exception payOSException)
            {
                _logger.LogError(payOSException, "❌ PayOS createPaymentLink failed for FacilityId {FacilityId}. Error: {ErrorMessage}", 
                    appointmentInfo.FacilityId, payOSException.Message);
                throw;
            }

            // Tạo Transaction record
            var transaction = new Transaction
            {
                TransactionType = paymentType,
                Amount = amount,
                PaymentMethod = "PAYOS",
                TransactionCode = orderCode,
                Description = description,
                Status = "PENDING",
                CreatedAt = DateOnly.FromDateTime(DateTime.UtcNow)
            };

            var transactionRepo = _unitOfWork.GetRepository<Transaction>();
            await transactionRepo.AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            _logger.LogInformation("✅ Tạo facility payment thành công. OrderCode: {OrderCode}, PaymentType: {PaymentType}, PaymentUrl: {PaymentUrl}", 
                orderCode, paymentType, createPayment.checkoutUrl);

            return new FacilityPaymentResponseDTO
            {
                PaymentUrl = createPayment.checkoutUrl,
                OrderCode = orderCode,
                Amount = amount,
                Status = "PENDING",
                AppointmentId = request.AppointmentId,
                PaymentType = paymentType,
                Description = description,
                OrderId = orderId,
                TransactionId = transaction.TransactionId
            };
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "Lỗi khi tạo facility payment cho AppointmentId: {AppointmentId}", request.AppointmentId);
            throw;
        }
    }

    /// <summary>
    /// Kiểm tra trạng thái thanh toán và cập nhật
    /// </summary>
    public async Task<PaymentStatusDTO> CheckFacilityPaymentStatusAsync(string orderCodeOrTimestamp)
    {
        try
        {
            string fullOrderCode;
            int facilityId;
            
            // Kiểm tra nếu input chỉ là timestamp (VD: 1755787333854) hoặc full orderCode (VD: 1755787333854_5_170_2)
            var inputParts = orderCodeOrTimestamp.Split('_');
            
            if (inputParts.Length == 1)
            {
                // Chỉ có timestamp, tìm transaction bằng cách search LIKE
                _logger.LogInformation("Searching transaction by timestamp: {Timestamp}", orderCodeOrTimestamp);
                
                var transactionRepo = _unitOfWork.GetRepository<Transaction>();
                var transaction = await transactionRepo.GetAsync(t => t.TransactionCode.StartsWith(orderCodeOrTimestamp + "_"));
                
                if (transaction == null)
                {
                    throw new KeyNotFoundException($"Không tìm thấy transaction với timestamp: {orderCodeOrTimestamp}");
                }
                
                fullOrderCode = transaction.TransactionCode;
                
                // Extract facilityId từ full orderCode
                var orderParts = fullOrderCode.Split('_');
                if (orderParts.Length < 3 || !int.TryParse(orderParts[1], out facilityId))
                {
                    throw new ArgumentException($"OrderCode format không hợp lệ: {fullOrderCode}");
                }
                
                _logger.LogInformation("Found transaction: {FullOrderCode} for timestamp: {Timestamp}", 
                    fullOrderCode, orderCodeOrTimestamp);
            }
            else
            {
                // Full orderCode format: {timestamp}_{facilityId}_{appointmentId}_{orderId}
                if (inputParts.Length < 3)
                {
                    throw new ArgumentException($"OrderCode format không hợp lệ: {orderCodeOrTimestamp}. Expected format: timestamp_facilityId_appointmentId[_orderId] or just timestamp");
                }

                if (!int.TryParse(inputParts[1], out facilityId))
                {
                    throw new ArgumentException($"FacilityId trong OrderCode không hợp lệ: {inputParts[1]}");
                }
                
                fullOrderCode = orderCodeOrTimestamp;
                
                var transactionRepo = _unitOfWork.GetRepository<Transaction>();
                var transaction = await transactionRepo.GetAsync(t => t.TransactionCode == fullOrderCode);
                
                if (transaction == null)
                {
                    throw new KeyNotFoundException($"Không tìm thấy transaction với OrderCode: {fullOrderCode}");
                }
            }

            _logger.LogInformation("Kiểm tra trạng thái facility payment - Input: {Input}, FullOrderCode: {FullOrderCode}, FacilityId: {FacilityId}", 
                orderCodeOrTimestamp, fullOrderCode, facilityId);

            var transactionRepo2 = _unitOfWork.GetRepository<Transaction>();
            var finalTransaction = await transactionRepo2.GetAsync(t => t.TransactionCode == fullOrderCode);
            
            if (finalTransaction == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy transaction với FullOrderCode: {fullOrderCode}");
            }

            // Chỉ check PayOS nếu chưa PAID
            if (!string.Equals(finalTransaction.Status, "PAID", StringComparison.OrdinalIgnoreCase))
            {
                var payOS = await GetFacilityPayOSInstanceAsync(facilityId);
                var payInfo = await payOS.getPaymentLinkInformation(long.Parse(fullOrderCode.Split('_')[0]));
                
                _logger.LogInformation("PayOS status: {Status} for OrderCode: {OrderCode}", payInfo.status, fullOrderCode);

                if (payInfo.status.Equals("PAID", StringComparison.OrdinalIgnoreCase))
                {
                    finalTransaction.Status = "PAID";
                    finalTransaction.Amount = payInfo.amount;
                    transactionRepo2.Update(finalTransaction);

                    // Cập nhật appointment status thành Paid
                    await UpdateAppointmentToPaid(fullOrderCode);
                    
                    await _unitOfWork.SaveChangesAsync();
                    _logger.LogInformation("✅ Transaction {OrderCode} updated to PAID", fullOrderCode);
                }
                else if (payInfo.status.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase))
                {
                    finalTransaction.Status = "CANCELLED";
                    transactionRepo2.Update(finalTransaction);
                    await _unitOfWork.SaveChangesAsync();
                    _logger.LogInformation("❌ Transaction {OrderCode} updated to CANCELLED", fullOrderCode);
                }
            }

            return new PaymentStatusDTO
            {
                Success = string.Equals(finalTransaction.Status, "PAID", StringComparison.OrdinalIgnoreCase),
                Status = finalTransaction.Status,
                Message = GetPaymentStatusMessage(finalTransaction.Status),
                Amount = finalTransaction.Amount,
                PaidAt = string.Equals(finalTransaction.Status, "PAID", StringComparison.OrdinalIgnoreCase) ? DateTime.UtcNow : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi kiểm tra trạng thái facility payment - Input: {Input}", orderCodeOrTimestamp);
            throw;
        }
    }

    #endregion

    #region Private Helper Methods

    private async Task ValidateFacilityAccess(int accountId, int facilityId)
    {
        var staffRepository = _unitOfWork.GetRepository<FacilityStaff>();
        var staff = await staffRepository.GetAsync(s => s.AccountId == accountId && s.FacilityId == facilityId);
        
        if (staff == null)
        {
            throw new UnauthorizedAccessException($"User {accountId} không có quyền truy cập facility {facilityId}");
        }
    }

    /// <summary>
    /// Lấy thông tin appointment và validate
    /// </summary>
    private async Task<AppointmentInfo> GetAppointmentInfoAsync(int appointmentId)
    {
        var appointmentRepo = _unitOfWork.GetRepository<VaccinationAppointment>();
        var appointment = await appointmentRepo.GetAsync(
            a => a.AppointmentId == appointmentId,
            includeProperties: "Schedule,Child,Order,VaccinationAppointmentDetails.Vaccine");

        if (appointment == null)
        {
            throw new ArgumentException($"Không tìm thấy Appointment {appointmentId}");
        }

        if (appointment.Schedule == null)
        {
            throw new InvalidOperationException($"Appointment {appointmentId} không có Schedule");
        }

        if (appointment.Status != "Approval")
        {
            throw new InvalidOperationException($"Appointment {appointmentId} phải có status 'Approval' mới có thể thanh toán. Status hiện tại: {appointment.Status}");
        }

        return new AppointmentInfo
        {
            AppointmentId = appointmentId,
            FacilityId = appointment.Schedule.FacilityId,
            ChildId = appointment.ChildId,
            OrderId = appointment.OrderId,
            Order = appointment.Order,
            VaccinationDetails = appointment.VaccinationAppointmentDetails?.ToList() ?? new List<VaccinationAppointmentDetail>()
        };
    }

    /// <summary>
    /// Tính toán payment dựa trên thông tin appointment
    /// </summary>
    private async Task<(decimal amount, string description, string paymentType, int? orderId)> CalculatePaymentFromAppointment(AppointmentInfo appointmentInfo)
    {
        // Nếu có OrderId - thanh toán cho Order đã tồn tại
        if (appointmentInfo.OrderId.HasValue && appointmentInfo.Order != null)
        {
            if (appointmentInfo.Order.Status == "Paid")
            {
                throw new InvalidOperationException($"Order {appointmentInfo.OrderId} đã được thanh toán");
            }

            return (
                appointmentInfo.Order.TotalAmount,
                $"Thanh toan don hang #{appointmentInfo.OrderId}",
                "ORDER",
                appointmentInfo.OrderId
            );
        }

        // Nếu không có OrderId - thanh toán vaccine lẻ từ VaccinationAppointmentDetails
        if (!appointmentInfo.VaccinationDetails.Any())
        {
            throw new InvalidOperationException($"Appointment {appointmentInfo.AppointmentId} không có thông tin vaccine để thanh toán");
        }

        // Tính tổng tiền vaccine lẻ
        decimal totalAmount = 0;
        var vaccineNames = new List<string>();
        var facilityVaccineRepo = _unitOfWork.GetRepository<FacilityVaccine>();

        foreach (var detail in appointmentInfo.VaccinationDetails)
        {
            // Tìm FacilityVaccine để lấy giá - sửa lại query
            var facilityVaccines = await facilityVaccineRepo.FindAsync(
                fv => fv.VaccineId == detail.VaccineId && fv.FacilityId == appointmentInfo.FacilityId,
                includeProperties: "Vaccine");

            var facilityVaccine = facilityVaccines.FirstOrDefault();
            
            if (facilityVaccine != null)
            {
                totalAmount += facilityVaccine.Price;
                vaccineNames.Add(facilityVaccine.Vaccine?.Name ?? "Unknown Vaccine");
            }
            else
            {
                _logger.LogWarning("Không tìm thấy FacilityVaccine cho VaccineId {VaccineId} tại Facility {FacilityId}", 
                    detail.VaccineId, appointmentInfo.FacilityId);
                // Fallback: sử dụng tên vaccine từ detail hoặc giá mặc định
                vaccineNames.Add(detail.Vaccine?.Name ?? "Unknown Vaccine");
                // Có thể set giá mặc định hoặc throw exception tùy business logic
                totalAmount += 0; // Hoặc một giá mặc định
            }
        }

        var description = $"Vaccine le: {string.Join(", ", vaccineNames)}";

        return (totalAmount, description, "INDIVIDUAL_VACCINE", null);
    }

    /// <summary>
    /// Class để chứa thông tin appointment
    /// </summary>
    private class AppointmentInfo
    {
        public int AppointmentId { get; set; }
        public int FacilityId { get; set; }
        public int ChildId { get; set; }
        public int? OrderId { get; set; }
        public Order? Order { get; set; }
        public List<VaccinationAppointmentDetail> VaccinationDetails { get; set; } = new();
    }

    private async Task UpdateAppointmentToPaid(string orderCode)
    {
        // Parse appointmentId từ orderCode
        var parts = orderCode.Split('_');
        if (parts.Length >= 3 && int.TryParse(parts[2], out var appointmentId))
        {
            var appointmentRepo = _unitOfWork.GetRepository<VaccinationAppointment>();
            var appointment = await appointmentRepo.GetByIdAsync(appointmentId);
            
            if (appointment != null && appointment.Status == "Approval")
            {
                appointment.Status = "Paid";
                appointment.UpdatedAt = DateTime.UtcNow;
                appointmentRepo.Update(appointment);
                
                _logger.LogInformation("✅ Updated Appointment {AppointmentId} status to Paid", appointmentId);

                // ✅ Cập nhật Order status nếu appointment có OrderId
                if (appointment.OrderId.HasValue)
                {
                    var orderRepo = _unitOfWork.GetRepository<Order>();
                    var order = await orderRepo.GetAsync(o => o.OrderId == appointment.OrderId.Value);

                    if (order != null && order.Status == "Pending")
                    {
                        order.Status = "Paid";
                        order.UpdatedAt = DateTime.UtcNow;
                        orderRepo.Update(order);
                        _logger.LogInformation("✅ Updated Order {OrderId} status từ Pending sang Paid", order.OrderId);
                    }
                    else if (order != null)
                    {
                        _logger.LogInformation("Order {OrderId} đã có status {Status}, không cần cập nhật", order.OrderId, order.Status);
                    }
                    else
                    {
                        _logger.LogWarning("❌ Không tìm thấy Order {OrderId} cho appointment {AppointmentId}", appointment.OrderId.Value, appointmentId);
                    }
                }
                else
                {
                    _logger.LogInformation("Appointment {AppointmentId} không có OrderId (thanh toán vaccine lẻ)", appointmentId);
                }

                // Note: VaccinationAppointmentDetails.VaccinationDate và ChildVaccineProfile.Status 
                // sẽ được cập nhật bởi Complete Vaccination API, không cần làm ở đây
            }
        }
    }

    private string GenerateOrderCode(int facilityId, int appointmentId, int? orderId)
    {
        var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        return orderId.HasValue 
            ? $"{timestamp}_{facilityId}_{appointmentId}_{orderId}"
            : $"{timestamp}_{facilityId}_{appointmentId}";
    }

    private string TruncateDescription(string description)
    {
        return description.Length > 25 ? description.Substring(0, 25) : description;
    }

    private string GetPaymentStatusMessage(string status)
    {
        return status switch
        {
            "PAID" => "Thanh toán thành công",
            "CANCELLED" => "Thanh toán đã bị hủy",
            "PENDING" => "Đang chờ thanh toán",
            "FAILED" => "Thanh toán thất bại",
            _ => "Trạng thái không xác định"
        };
    }

    private string GetBaseUrl()
    {
        return _configuration["BaseUrl"] ?? "https://localhost:7000";
    }

    private string GetFrontendUrl()
    {
        return _configuration["FrontendUrl"] ?? "http://localhost:5173";
    }

    #endregion
}