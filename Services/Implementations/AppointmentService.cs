using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using BusinessLogic.DTOs.Appointment;
using BusinessLogic.Services.Interfaces;
using DataAccess.Entities;
using DataAccess.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Services.Implementations
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<AppointmentService> _logger;

        public AppointmentService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<AppointmentService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }


        public async Task<IEnumerable<AppointmentDTO>> GetUserAppointmentsAsync(int userId)
        {
            try
            {
                var appointmentRepository = _unitOfWork.GetRepository<Appointment>();
                var appointments = await appointmentRepository.FindAsync(
                    a => a.UserId == userId,
                    includeProperties: "User,Child,Schedule,Schedule.Doctor"
                );

                var appointmentDTOs = _mapper.Map<IEnumerable<AppointmentDTO>>(appointments);
                foreach (var dto in appointmentDTOs)
                {
                    var appointment = appointments.FirstOrDefault(a => a.AppointmentId == dto.AppointmentId);
                    if (appointment?.Schedule != null)
                    {
                        dto.AppointmentDate = appointment.Schedule.WorkDate;
                        dto.DoctorId = appointment.Schedule.DoctorId;
                        dto.DoctorName = appointment.Schedule.Doctor?.FullName;

                        // Chuyển đổi SlotTime thành thời gian tương ứng
                        if (int.TryParse(appointment.SlotTime, out int slotId))
                        {
                            var timeSlot = DoctorScheduleService.DEFAULT_SLOTS.FirstOrDefault(s => s.SlotId == slotId);
                            if (timeSlot != null)
                            {
                                dto.AppointmentTime = timeSlot.StartTime; // Thêm property này vào AppointmentDTO
                            }
                        }
                    }
                }

                return appointmentDTOs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy danh sách lịch hẹn của người dùng {userId}");
                throw;
            }
        }

        public async Task<IEnumerable<AppointmentDTO>> GetDoctorAppointmentsAsync(int doctorId)
        {
            try
            {
                var appointmentRepository = _unitOfWork.GetRepository<Appointment>();
                var appointments = await appointmentRepository.FindAsync(
                    a => a.Schedule.DoctorId == doctorId,
                    includeProperties: "User,Child,Schedule,Schedule.Doctor"
                );

                var appointmentDTOs = _mapper.Map<IEnumerable<AppointmentDTO>>(appointments);
                foreach (var dto in appointmentDTOs)
                {
                    var appointment = appointments.FirstOrDefault(a => a.AppointmentId == dto.AppointmentId);
                    if (appointment?.Schedule != null)
                    {
                        dto.AppointmentDate = appointment.Schedule.WorkDate;
                        dto.DoctorId = appointment.Schedule.DoctorId;
                        dto.DoctorName = appointment.Schedule.Doctor?.FullName;
                    }
                }

                return appointmentDTOs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy danh sách lịch hẹn của bác sĩ {doctorId}");
                throw;
            }
        }

        public async Task<AppointmentDTO> GetAppointmentByIdAsync(int appointmentId)
        {
            try
            {
                var appointmentRepository = _unitOfWork.GetRepository<Appointment>();
                var appointment = await appointmentRepository.GetAsync(
                    a => a.AppointmentId == appointmentId,
                    includeProperties: "User,Child,Schedule,Schedule.Doctor"
                );

                if (appointment == null)
                {
                    throw new KeyNotFoundException($"Không tìm thấy lịch hẹn với ID {appointmentId}");
                }

                var appointmentDTO = _mapper.Map<AppointmentDTO>(appointment);
                if (appointment.Schedule != null)
                {
                    appointmentDTO.AppointmentDate = appointment.Schedule.WorkDate;
                    appointmentDTO.DoctorId = appointment.Schedule.DoctorId;
                    appointmentDTO.DoctorName = appointment.Schedule.Doctor?.FullName;
                }

                return appointmentDTO;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy thông tin lịch hẹn {appointmentId}");
                throw;
            }
        }
        public async Task<AppointmentDTO> CreateAppointmentAsync(CreateAppointmentDTO appointmentDTO)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // 1. Kiểm tra membership active và quyền đặt lịch
                var userMembershipRepo = _unitOfWork.GetRepository<UserMembership>();

                // Lấy tất cả membership của user để debug
                var allUserMemberships = await userMembershipRepo.FindAsync(
                    x => x.UserId == appointmentDTO.UserId,
                    includeProperties: "Membership"
                );

                _logger.LogInformation($"Tìm thấy {allUserMemberships.Count()} membership cho user {appointmentDTO.UserId}");

                // Kiểm tra membership active
                var userMembership = allUserMemberships.FirstOrDefault(x =>
                    x.Status == "Active" && // Status của UserMembership là string
                    x.EndDate > DateTime.UtcNow
                );

                if (userMembership == null)
                {
                    _logger.LogWarning($"User {appointmentDTO.UserId} không có membership active");
                    throw new InvalidOperationException("Bạn cần đăng ký gói membership để đặt lịch");
                }

                if (!userMembership.Membership.Status) // Status của Membership là bool
                {
                    _logger.LogWarning($"User {appointmentDTO.UserId} có membership không có quyền đặt lịch");
                    throw new InvalidOperationException("Gói membership của bạn không có quyền đặt lịch");
                }

                if (!userMembership.Membership.CanAccessAppoinment)
                {
                    _logger.LogWarning($"User {appointmentDTO.UserId} có membership không có quyền đặt lịch");
                    throw new InvalidOperationException("Gói membership của bạn không có quyền đặt lịch");
                }

                // 2. Kiểm tra số lần đặt lịch trong tháng
                var firstDayOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                var appointmentRepo = _unitOfWork.GetRepository<Appointment>();
                var appointmentThisMonth = await appointmentRepo.FindAsync(
                    a => a.UserId == appointmentDTO.UserId &&
                         a.CreatedAt >= firstDayOfMonth &&
                         a.CreatedAt <= lastDayOfMonth &&
                         a.Status != "Cancelled"
                );

                if (appointmentThisMonth.Count() >= 3)
                {
                    throw new InvalidOperationException("Bạn đã đạt giới hạn đặt lịch trong tháng này (tối đa 3 lần/tháng)");
                }

                // 3. Kiểm tra khoảng cách giữa các lần đặt lịch
                var previousAppointments = await appointmentRepo.FindAsync(
                    a => a.UserId == appointmentDTO.UserId &&
                         a.Status != "Cancelled"
                );

                var lastAppointment = previousAppointments
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefault();

                if (lastAppointment != null)
                {
                    var daysSinceLastAppointment = (DateTime.UtcNow - lastAppointment.CreatedAt).Days;
                    if (daysSinceLastAppointment < 7)
                    {
                        throw new InvalidOperationException($"Vui lòng đợi thêm {7 - daysSinceLastAppointment} ngày nữa để đặt lịch tiếp");
                    }
                }
                // Lấy thông tin lịch bác sĩ
                var scheduleRepository = _unitOfWork.GetRepository<DoctorSchedule>();
                var schedule = await scheduleRepository.GetAsync(
                    s => s.ScheduleId == appointmentDTO.ScheduleId,
                    includeProperties: "Doctor"
                );

                if (schedule == null)
                {
                    throw new KeyNotFoundException("Không tìm thấy lịch làm việc");
                }

                // Kiểm tra slot có tồn tại trong selectedSlots không
                var selectedSlots = schedule.SelectedSlots.Split(',').Select(int.Parse).ToList();
                var slotId = int.Parse(appointmentDTO.SlotTime);
                if (!selectedSlots.Contains(slotId))
                {
                    throw new InvalidOperationException("Slot không tồn tại trong lịch làm việc này");
                }

                // Kiểm tra thời gian slot có hợp lệ không
                var now = DateTime.Now;
                if (schedule.WorkDate < DateOnly.FromDateTime(now))
                {
                    throw new InvalidOperationException("Không thể đặt lịch cho ngày đã qua");
                }

                // Kiểm tra xem user đã từng hủy lịch trong ngày này chưa
                var appointmentRepository = _unitOfWork.GetRepository<Appointment>();
                var cancelledToday = await appointmentRepository.FindAsync(
                    a => a.UserId == appointmentDTO.UserId &&
                         a.Schedule.WorkDate == schedule.WorkDate &&
                         a.Status == "Cancelled"
                );

                if (cancelledToday.Any())
                {
                    throw new InvalidOperationException("Bạn đã hủy lịch hẹn trong ngày này và không thể đặt lại");
                }

                // Kiểm tra slot có trống không
                var existingAppointment = await appointmentRepository.GetAsync(
                    a => a.ScheduleId == appointmentDTO.ScheduleId &&
                         a.SlotTime == appointmentDTO.SlotTime &&
                         a.Status != "Cancelled"
                );

                if (existingAppointment != null)
                {
                    throw new InvalidOperationException("Slot này đã được đặt");
                }

                // Tạo link Google Meet
                string meetingLink = $"https://meet.google.com/{Guid.NewGuid().ToString("N").Substring(0, 12)}";

                // Tạo cuộc hẹn mới
                var appointment = new Appointment
                {
                    ScheduleId = appointmentDTO.ScheduleId,
                    UserId = appointmentDTO.UserId,
                    ChildId = appointmentDTO.ChildId,
                    SlotTime = appointmentDTO.SlotTime,
                    Status = "Pending",
                    MeetingLink = meetingLink,
                    Description = appointmentDTO.Description,
                    Note = appointmentDTO.Note ?? string.Empty,
                    CreatedAt = DateTime.UtcNow
                };

                await appointmentRepository.AddAsync(appointment);
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();

                // Lấy thông tin đầy đủ của cuộc hẹn
                var createdAppointment = await appointmentRepository.GetAsync(
                    a => a.AppointmentId == appointment.AppointmentId,
                    includeProperties: "User,Child,Schedule,Schedule.Doctor"
                );

                var result = _mapper.Map<AppointmentDTO>(createdAppointment);
                if (createdAppointment.Schedule != null)
                {
                    result.AppointmentDate = createdAppointment.Schedule.WorkDate;
                    result.DoctorId = createdAppointment.Schedule.DoctorId;
                    result.DoctorName = createdAppointment.Schedule.Doctor?.FullName;
                }

                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi tạo cuộc hẹn");
                throw;
            }
        }
        public async Task<bool> CancelAppointmentAsync(int appointmentId, int userId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var appointmentRepository = _unitOfWork.GetRepository<Appointment>();
                var appointment = await appointmentRepository.GetAsync(
                    a => a.AppointmentId == appointmentId && 
                         (a.UserId == userId || a.Schedule.DoctorId == userId),
                    includeProperties: "Schedule"
                );

                if (appointment == null)
                {
                    throw new KeyNotFoundException("Không tìm thấy cuộc hẹn hoặc bạn không có quyền hủy");
                }

                // Kiểm tra trạng thái cuộc hẹn
                if (appointment.Status == "Cancelled")
                {
                    throw new InvalidOperationException("Cuộc hẹn đã được hủy trước đó");
                }

                if (appointment.Status == "Completed")
                {
                    throw new InvalidOperationException("Không thể hủy cuộc hẹn đã hoàn thành");
                }

                // Kiểm tra thời gian hủy
                var now = DateTime.Now;
                if (appointment.Schedule.WorkDate < DateOnly.FromDateTime(now))
                {
                    throw new InvalidOperationException("Không thể hủy cuộc hẹn đã qua");
                }

                // Kiểm tra số lần hủy trong ngày
                var cancelledToday = await appointmentRepository.FindAsync(
                    a => a.UserId == userId &&
                         a.Schedule.WorkDate == appointment.Schedule.WorkDate &&
                         a.Status == "Cancelled"
                );

                if (cancelledToday.Any())
                {
                    throw new InvalidOperationException("Bạn đã hủy một cuộc hẹn khác trong ngày này");
                }

                appointment.Status = "Cancelled";
                appointmentRepository.Update(appointment);
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Lỗi khi hủy cuộc hẹn {appointmentId}");
                throw;
            }
        }

        public async Task<AppointmentDTO> CompleteAppointmentAsync(int appointmentId, string note, int doctorId)
        {
            try
            {
                _logger.LogInformation($"Bắt đầu hoàn thành cuộc hẹn {appointmentId} bởi bác sĩ {doctorId}");

                // Lấy thông tin bác sĩ từ DoctorProfile
                var doctorProfile = await _unitOfWork.GetRepository<DoctorProfile>()
                    .FindAsync(d => d.UserId == doctorId);

                if (doctorProfile == null || !doctorProfile.Any()) 
                {
                    _logger.LogWarning($"Không tìm thấy thông tin bác sĩ với UserId {doctorId}");
                    throw new UnauthorizedAccessException("Bạn không phải là bác sĩ hoặc không có quyền hoàn thành cuộc hẹn này");
                }

                var appointment = await _unitOfWork.GetRepository<Appointment>()
                    .GetByIdAsync(appointmentId);

                if (appointment == null)
                {
                    _logger.LogWarning($"Không tìm thấy cuộc hẹn {appointmentId}");
                    throw new KeyNotFoundException($"Không tìm thấy cuộc hẹn với ID {appointmentId}");
                }

                // Kiểm tra quyền truy cập thông qua Schedule
                var schedule = await _unitOfWork.GetRepository<DoctorSchedule>()
                    .GetByIdAsync(appointment.ScheduleId);

                if (schedule == null)
                {
                    _logger.LogWarning($"Không tìm thấy lịch làm việc cho cuộc hẹn {appointmentId}");
                    throw new KeyNotFoundException("Không tìm thấy lịch làm việc cho cuộc hẹn này");
                }

                if (schedule.DoctorId != doctorId)
                {
                    _logger.LogWarning($"Bác sĩ {doctorId} không có quyền hoàn thành cuộc hẹn {appointmentId}");
                    throw new UnauthorizedAccessException("Bạn không có quyền hoàn thành cuộc hẹn này");
                }

                // Kiểm tra trạng thái cuộc hẹn
                if (appointment.Status != "Scheduled" && appointment.Status != "Ongoing")
                {
                    _logger.LogWarning($"Cuộc hẹn {appointmentId} không thể hoàn thành với trạng thái {appointment.Status}");
                    throw new InvalidOperationException($"Không thể hoàn thành cuộc hẹn với trạng thái {appointment.Status}");
                }

                // Kiểm tra thời gian
                if (int.TryParse(appointment.SlotTime, out int slotId))
                {
                    // Tạo thời gian bắt đầu từ WorkDate và StartTime của schedule
                    DateTime appointmentStartTime = schedule.WorkDate.ToDateTime(schedule.StartTime, DateTimeKind.Unspecified)
                        .AddMinutes((slotId - 1) * schedule.SlotDuration);

                    // Chuyển đổi thời gian hiện tại sang UTC+7
                    DateTime vietnamTime = DateTime.UtcNow.AddHours(7);

                    if (appointmentStartTime > vietnamTime)
                    {
                        _logger.LogWarning($"Không thể hoàn thành cuộc hẹn {appointmentId} trước thời gian bắt đầu ({appointmentStartTime:dd/MM/yyyy HH:mm})");
                        throw new InvalidOperationException("Không thể hoàn thành cuộc hẹn trước thời gian bắt đầu");
                    }
                }
                else
                {
                    _logger.LogWarning($"SlotTime '{appointment.SlotTime}' không hợp lệ cho cuộc hẹn {appointmentId}");
                    throw new InvalidOperationException("Thời gian cuộc hẹn không hợp lệ");
                }

                // Cập nhật thông tin cuộc hẹn
                appointment.Status = "Completed";
                appointment.Note = note;
                appointment.CreatedAt = DateTime.UtcNow.AddHours(7); // Cập nhật thời gian tạo theo UTC+7

                _unitOfWork.GetRepository<Appointment>().Update(appointment);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Đã hoàn thành cuộc hẹn {appointmentId} thành công");
                return _mapper.Map<AppointmentDTO>(appointment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi hoàn thành cuộc hẹn {appointmentId}");
                throw;
            }
        }
    }
}