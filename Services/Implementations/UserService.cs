using AutoMapper;
using BusinessLogic.DTOs.User;
using BusinessLogic.Services.Interfaces;
using DataAccess.Entities;
using DataAccess.UnitOfWork;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<UserService> _logger;

        public UserService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<UserService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<UserDTO>> GetAllUsersAsync()
        {
            var users = await _unitOfWork.GetRepository<User>().GetAllAsync();
            return _mapper.Map<List<UserDTO>>(users);
        }

        public async Task<UserDTO> GetCurrentUserDetailAsync(int currentUserId)
        {
            var user = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            if (user == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy thông tin người dùng hiện tại");
            }
            return _mapper.Map<UserDTO>(user);
        }

        public async Task<UserDTO> UpdateUserProfileAsync(int userId, UpdateUserProfileDTO updateUserDTO)
        {
            var userRepo = _unitOfWork.GetRepository<User>();
            var user = await userRepo.GetByIdAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy người dùng với ID: {userId}");
            }

            _mapper.Map(updateUserDTO, user);
            user.UpdatedAt = DateTime.UtcNow;

            userRepo.Update(user);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<UserDTO>(user);
        }

        public async Task<bool> UpdateUserStatusAsync(int userId, bool status)
        {
            try
            {
                var userRepo = _unitOfWork.GetRepository<User>();
                var user = await userRepo.GetByIdAsync(userId);
                if (user == null)
                {
                    throw new KeyNotFoundException($"Không tìm thấy người dùng với ID: {userId}");
                }

                // Nếu đang cập nhật status thành false (vô hiệu hóa) và là bác sĩ
                if (!status && user.Role == "Doctor")
                {
                    _logger.LogInformation($"Kiểm tra lịch hẹn tương lai cho bác sĩ {userId} trước khi vô hiệu hóa.");
                    var appointmentRepo = _unitOfWork.GetRepository<Appointment>();
                    var now = DateTime.UtcNow;

                    // Tìm các lịch hẹn "Scheduled" hoặc "Ongoing" của bác sĩ này
                    var activeAppointments = await appointmentRepo.FindAsync(
                        a => a.Schedule.DoctorId == userId &&
                             (a.Status == "Scheduled" || a.Status == "Ongoing"),
                        includeProperties: "Schedule"
                    );

                    foreach (var appt in activeAppointments)
                    {
                        if (appt.Schedule == null)
                        {
                            _logger.LogWarning($"Appointment {appt.AppointmentId} không có thông tin Schedule khi kiểm tra ban bác sĩ {userId}.");
                            continue;
                        }

                        if (TimeSpan.TryParseExact(appt.SlotTime, "hh\\:mm", CultureInfo.InvariantCulture, out var startTimeSpan))
                        {
                            DateTime appointmentStartTime = appt.Schedule.WorkDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified).Add(startTimeSpan);

                            if (appointmentStartTime > now)
                            {
                                _logger.LogWarning($"Phát hiện lịch hẹn tương lai ({appointmentStartTime:dd/MM/yyyy HH:mm}) cho bác sĩ {userId}.");
                                throw new InvalidOperationException(
                                    $"Không thể vô hiệu hóa tài khoản bác sĩ này vì còn lịch hẹn trong tương lai chưa được xử lý. " +
                                    $"Vui lòng hủy hoặc hoàn thành các lịch hẹn trước."
                                );
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"Định dạng SlotTime '{appt.SlotTime}' của lịch hẹn {appt.AppointmentId} không hợp lệ.");
                        }
                    }
                }

                // Cập nhật trạng thái
                user.Status = status;
                user.UpdatedAt = DateTime.UtcNow;

                userRepo.Update(user);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Đã cập nhật trạng thái của người dùng {userId} thành {status}.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi cập nhật trạng thái người dùng {userId}");
                throw;
            }
        }
    }
}
