using AutoMapper;
using BusinessLogic.DTOs.Doctor;
using BusinessLogic.Services.Interfaces;
using DataAccess.Entities;
using DataAccess.UnitOfWork;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace BusinessLogic.Services.Implementations
{
    public class DoctorService : IDoctorService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<DoctorService> _logger;

        public DoctorService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<DoctorService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<DoctorDTO>> GetAllDoctorsAsync()
        {
            try
            {
                var userRepository = _unitOfWork.GetRepository<User>();
                var doctors = await userRepository.FindAsync(
                    u => u.Role == "Doctor",
                    includeProperties: "DoctorProfiles");
                return _mapper.Map<IEnumerable<DoctorDTO>>(doctors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách bác sĩ");
                throw;
            }
        }

        public async Task<DoctorDTO> GetDoctorByIdAsync(int doctorId)
        {
            try
            {
                var userRepository = _unitOfWork.GetRepository<User>();
                var doctor = await userRepository.GetAsync(
                    u => u.UserId == doctorId && u.Role == "Doctor",
                    includeProperties: "DoctorProfiles");

                if (doctor == null)
                {
                    throw new KeyNotFoundException($"Không tìm thấy bác sĩ với ID {doctorId}");
                }

                return _mapper.Map<DoctorDTO>(doctor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy thông tin bác sĩ {doctorId}");
                throw;
            }
        }

        public async Task<DoctorDTO> CreateDoctorAsync(CreateDoctorDTO doctorDTO)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var userRepository = _unitOfWork.GetRepository<User>();
                
                // Kiểm tra username đã tồn tại
                var existingUsername = await userRepository.GetAsync(u => u.Username == doctorDTO.Username);
                if (existingUsername != null)
                {
                    throw new InvalidOperationException("Username đã tồn tại trong hệ thống");
                }

                // Kiểm tra email đã tồn tại
                var existingEmail = await userRepository.GetAsync(u => u.Email == doctorDTO.Email);
                if (existingEmail != null)
                {
                    throw new InvalidOperationException("Email đã tồn tại trong hệ thống");
                }

                // Tạo user mới
                var user = new User
                {
                    Username = doctorDTO.Username,
                    Email = doctorDTO.Email,
                    Password = BCrypt.Net.BCrypt.HashPassword(doctorDTO.Password),
                    FullName = doctorDTO.FullName,
                    Phone = doctorDTO.PhoneNumber,
                    Address = doctorDTO.Address,
                    Role = "Doctor",
                    Status = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await userRepository.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();

                // Tạo doctor profile
                var profileRepository = _unitOfWork.GetRepository<DoctorProfile>();
                var profile = new DoctorProfile
                {
                    UserId = user.UserId,
                    Specialization = doctorDTO.Specialization,
                    Qualification = doctorDTO.Qualification,
                    Biography = doctorDTO.Biography,
                    LicenseNumber = doctorDTO.LicenseNumber,                               
                    AverageRating = 0,
                    TotalRatings = 0,
                    IsVerified = true,
                };

                await profileRepository.AddAsync(profile);
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();

                // Lấy thông tin doctor vừa tạo
                var createdDoctor = await GetDoctorByIdAsync(user.UserId);
                return createdDoctor;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi tạo bác sĩ mới");
                throw;
            }
        }

        public async Task<DoctorDTO> UpdateDoctorAsync(int doctorId, UpdateDoctorDTO doctorDTO)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var userRepository = _unitOfWork.GetRepository<User>();
                var doctor = await userRepository.GetAsync(u => u.UserId == doctorId && u.Role == "Doctor");

                if (doctor == null)
                {
                    throw new KeyNotFoundException($"Không tìm thấy bác sĩ với ID {doctorId}");
                }

                // Cập nhật thông tin user
                doctor.FullName = doctorDTO.FullName;
                doctor.Phone = doctorDTO.PhoneNumber;
                doctor.Address = doctorDTO.Address;
                doctor.UpdatedAt = DateTime.UtcNow;

                userRepository.Update(doctor);
                await _unitOfWork.SaveChangesAsync();

                // Cập nhật doctor profile
                var profileRepository = _unitOfWork.GetRepository<DoctorProfile>();
                var profile = await profileRepository.GetAsync(p => p.UserId == doctorId);

                if (profile != null)
                {
                    profile.Specialization = doctorDTO.Specialization;
                    profile.Qualification = doctorDTO.Qualification;
                    profile.Biography = doctorDTO.Biography;
                    profile.LicenseNumber = doctorDTO.LicenseNumber;
                    profile.Experience = doctorDTO.Experience;
                    profile.IsVerified = true;

                    profileRepository.Update(profile);
                    await _unitOfWork.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                // Lấy thông tin doctor sau khi cập nhật
                return await GetDoctorByIdAsync(doctorId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Lỗi khi cập nhật thông tin bác sĩ {doctorId}");
                throw;
            }
        }

        public async Task<bool> ToggleDoctorVerification(int doctorId)
        {
            try
            {
                var userRepository = _unitOfWork.GetRepository<User>();
                var doctorProfileRepository = _unitOfWork.GetRepository<DoctorProfile>();

                // Lấy thông tin bác sĩ
                var doctor = await userRepository.GetAsync(
                    u => u.UserId == doctorId && u.Role == "Doctor",
                    includeProperties: "DoctorProfiles"
                );

                if (doctor == null)
                {
                    throw new KeyNotFoundException($"Không tìm thấy bác sĩ với ID {doctorId}");
                }

                if (!doctor.DoctorProfiles.Any())
                {
                    throw new InvalidOperationException($"Bác sĩ với ID {doctorId} chưa có hồ sơ");
                }

                var doctorProfile = doctor.DoctorProfiles.First();

                // Đảo ngược trạng thái xác thực
                doctorProfile.IsVerified = !doctorProfile.IsVerified;
                doctor.Status = doctorProfile.IsVerified;
                doctor.UpdatedAt = DateTime.UtcNow;

                doctorProfileRepository.Update(doctorProfile);
                userRepository.Update(doctor);
                await _unitOfWork.SaveChangesAsync();

                return doctorProfile.IsVerified;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi cập nhật trạng thái xác thực của bác sĩ {doctorId}");
                throw;
            }
        }
    }
}
