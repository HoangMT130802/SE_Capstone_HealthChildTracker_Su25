using AutoMapper;
using Contracts.DTOs.ChildVaccineProfile;
using Microsoft.Extensions.Logging;
using Repositories.Entities;
using Repositories.Interfaces;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Services.Implementations
{
    public class ChildVaccineProfileService : IChildVaccineProfileService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<ChildVaccineProfileService> _logger;

        public ChildVaccineProfileService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<ChildVaccineProfileService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<ChildVaccineProfileDTO>> GetAllChildVaccineProfilesByChildIdAsync(int childId)
        {
            try
            {
                var profileRepository = _unitOfWork.GetRepository<ChildVaccineProfile>();
                var profiles = await profileRepository.FindAsync(r => r.ChildId == childId, includeProperties: "Child,Vaccine");
                return _mapper.Map<IEnumerable<ChildVaccineProfileDTO>>(profiles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting vaccine profiles for child {childId}");
                throw;
            }
        }

        /// <summary>
        /// Lấy tất cả vaccine profiles của child mà không cần check account ownership (public API)
        /// </summary>
        public async Task<IEnumerable<ChildVaccineProfileDTO>> GetAllChildVaccineProfilesByChildIdPublicAsync(int childId)
        {
            try
            {
                // Kiểm tra xem child có tồn tại và đang active không
                var childRepository = _unitOfWork.GetRepository<Child>();
                var child = await childRepository.GetAsync(c => c.ChildId == childId && c.Status == true);
                
                if (child == null)
                {
                    throw new KeyNotFoundException($"Child with ID {childId} not found or inactive");
                }

                var profileRepository = _unitOfWork.GetRepository<ChildVaccineProfile>();
                var profiles = await profileRepository.FindAsync(r => r.ChildId == childId, includeProperties: "Child,Vaccine");
                return _mapper.Map<IEnumerable<ChildVaccineProfileDTO>>(profiles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting public vaccine profiles for child {childId}");
                throw;
            }
        }

        public async Task<ChildVaccineProfileDTO> GetChildVaccineProfileByIdAsync(int profileId)
        {
            try
            {
                var profileRepository = _unitOfWork.GetRepository<ChildVaccineProfile>();
                var profile = await profileRepository.GetAsync(r => r.VaccineProfileId == profileId, includeProperties: "Child,Vaccine,Disease");

                if (profile == null)
                {
                    throw new KeyNotFoundException($"Vaccine profile with ID {profileId} not found");
                }

                return _mapper.Map<ChildVaccineProfileDTO>(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting vaccine profile {profileId}");
                throw;
            }
        }

        public async Task<ChildVaccineProfileDTO> CreateChildVaccineProfileAsync(CreateChildVaccineProfileDTO profileDTO)
        {
            try
            {
                // Validate child exists
                var childRepository = _unitOfWork.GetRepository<Child>();
                var child = await childRepository.GetAsync(c => c.ChildId == profileDTO.ChildId);
                if (child == null)
                {
                    throw new KeyNotFoundException($"Child with ID {profileDTO.ChildId} not found");
                }

                // Validate vaccine exists
                var vaccineRepository = _unitOfWork.GetRepository<Vaccine>();
                var vaccine = await vaccineRepository.GetAsync(v => v.VaccineId == profileDTO.VaccineId);
                if (vaccine == null)
                {
                    throw new KeyNotFoundException($"Vaccine with ID {profileDTO.VaccineId} not found");
                }

                // Validate disease exists
                var diseaseRepository = _unitOfWork.GetRepository<Disease>();
                var disease = await diseaseRepository.GetAsync(d => d.DiseaseId == profileDTO.DiseaseId);
                if (disease == null)
                {
                    throw new KeyNotFoundException($"Disease with ID {profileDTO.DiseaseId} not found");
                }

                // Validate expected date
                var birthDate = child.BirthDate.Date;
                var expectedDate = profileDTO.ExpectedDate.ToDateTime(TimeOnly.MinValue);
                if (expectedDate < birthDate)
                {
                    throw new InvalidOperationException($"Cannot set expected date before child's birth date. Birth date: {child.BirthDate:dd/MM/yyyy}");
                }

                // Validate actual date if provided
                if (profileDTO.ActualDate.HasValue)
                {
                    var actualDate = profileDTO.ActualDate.Value.ToDateTime(TimeOnly.MinValue);
                    if (actualDate < birthDate)
                    {
                        throw new InvalidOperationException($"Cannot set actual date before child's birth date. Birth date: {child.BirthDate:dd/MM/yyyy}");
                    }
                    if (actualDate > DateTime.UtcNow.Date)
                    {
                        throw new InvalidOperationException($"Cannot set actual date in the future. Actual date: {actualDate:dd/MM/yyyy}");
                    }
                }

                // Validate no existing profile for same child, vaccine, and dose
                var profileRepository = _unitOfWork.GetRepository<ChildVaccineProfile>();
                var existingProfile = await profileRepository.GetAsync(p =>
                    p.ChildId == profileDTO.ChildId &&
                    p.VaccineId == profileDTO.VaccineId &&
                    p.DoseNum == profileDTO.DoseNum);

                if (existingProfile != null)
                {
                    throw new InvalidOperationException($"A vaccine profile already exists for child ID {profileDTO.ChildId}, vaccine ID {profileDTO.VaccineId}, dose {profileDTO.DoseNum}");
                }

                var profile = _mapper.Map<ChildVaccineProfile>(profileDTO);
                profile.CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                profile.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                await profileRepository.AddAsync(profile);
                await _unitOfWork.SaveChangesAsync();

                var savedProfile = await profileRepository.GetAsync(
                    p => p.VaccineProfileId == profile.VaccineProfileId,
                    includeProperties: "Child,Vaccine,Disease"
                );

                return _mapper.Map<ChildVaccineProfileDTO>(savedProfile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating vaccine profile for child {profileDTO.ChildId}");
                throw;
            }
        }

        public async Task<ChildVaccineProfileDTO> UpdateChildVaccineProfileAsync(int profileId, UpdateChildVaccineProfileDTO profileDTO)
        {
            try
            {
                var profileRepository = _unitOfWork.GetRepository<ChildVaccineProfile>();
                var profile = await profileRepository.GetAsync(
                    p => p.VaccineProfileId == profileId,
                    includeProperties: "Child,Vaccine,Disease"
                );

                if (profile == null)
                {
                    throw new KeyNotFoundException($"Vaccine profile with ID {profileId} not found");
                }

                // Validate expected date
                var birthDate = profile.Child.BirthDate.Date;
                var expectedDate = profileDTO.ExpectedDate.ToDateTime(TimeOnly.MinValue);
                if (expectedDate < birthDate)
                {
                    throw new InvalidOperationException($"Cannot set expected date before child's birth date. Birth date: {profile.Child.BirthDate:dd/MM/yyyy}");
                }

                // Validate actual date if provided
                if (profileDTO.ActualDate.HasValue)
                {
                    var actualDate = profileDTO.ActualDate.Value.ToDateTime(TimeOnly.MinValue);
                    if (actualDate < birthDate)
                    {
                        throw new InvalidOperationException($"Cannot set actual date before child's birth date. Birth date: {profile.Child.BirthDate:dd/MM/yyyy}");
                    }
                    if (actualDate > DateTime.UtcNow.Date)
                    {
                        throw new InvalidOperationException($"Cannot set actual date in the future. Actual date: {actualDate:dd/MM/yyyy}");
                    }
                }

                // Validate disease if provided
                if (profileDTO.DiseaseId.HasValue)
                {
                    var diseaseRepository = _unitOfWork.GetRepository<Disease>();
                    var disease = await diseaseRepository.GetAsync(d => d.DiseaseId == profileDTO.DiseaseId.Value);
                    if (disease == null)
                    {
                        throw new KeyNotFoundException($"Disease with ID {profileDTO.DiseaseId} not found");
                    }
                }

                _mapper.Map(profileDTO, profile);
                profile.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                profileRepository.Update(profile);
                await _unitOfWork.SaveChangesAsync();

                var updatedProfile = await profileRepository.GetAsync(
                    p => p.VaccineProfileId == profileId,
                    includeProperties: "Child,Vaccine,Disease"
                );

                return _mapper.Map<ChildVaccineProfileDTO>(updatedProfile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating vaccine profile {profileId}");
                throw;
            }
        }

        public async Task<bool> DeleteChildVaccineProfileAsync(int profileId)
        {
            try
            {
                var profileRepository = _unitOfWork.GetRepository<ChildVaccineProfile>();
                var profile = await profileRepository.GetAsync(p => p.VaccineProfileId == profileId);

                if (profile == null)
                {
                    throw new KeyNotFoundException($"Vaccine profile with ID {profileId} not found");
                }

                profileRepository.Delete(profile);
                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting vaccine profile {profileId}");
                throw;
            }
        }

        /// <summary>
        /// Doctor ghi nhận hoàn thành tiêm vaccine và tạo mũi tiếp theo nếu cần
        /// </summary>
        public async Task<VaccinationCompletionResponseDTO> CompleteVaccinationAsync(CompleteVaccinationDTO completeDto)
        {
            try
            {
                _logger.LogInformation("Doctor completing vaccination for Child {ChildId}, Vaccine {VaccineId}, Dose {DoseNumber}", 
                    completeDto.ChildId, completeDto.VaccineId, completeDto.DoseNumber);

                // 1. Validate appointment có status "Payed" không
                var appointmentRepo = _unitOfWork.GetRepository<VaccinationAppointment>();
                var appointment = await appointmentRepo.GetAsync(a => a.AppointmentId == completeDto.AppointmentId);
                if (appointment == null || appointment.Status != "Payed")
                {
                    throw new InvalidOperationException($"Appointment {completeDto.AppointmentId} not found or not in Payed status");
                }

                // 2. Validate child, vaccine exists
                var childRepository = _unitOfWork.GetRepository<Child>();
                var child = await childRepository.GetAsync(c => c.ChildId == completeDto.ChildId);
                if (child == null)
                {
                    throw new KeyNotFoundException($"Child with ID {completeDto.ChildId} not found");
                }

                var vaccineRepository = _unitOfWork.GetRepository<Vaccine>();
                var vaccine = await vaccineRepository.GetAsync(v => v.VaccineId == completeDto.VaccineId, "VaccineDiseases");
                if (vaccine == null)
                {
                    throw new KeyNotFoundException($"Vaccine with ID {completeDto.VaccineId} not found");
                }

                // Lấy DiseaseId đầu tiên từ vaccine (có thể có nhiều disease)
                var diseaseId = vaccine.VaccineDiseases?.FirstOrDefault()?.DiseaseId ?? 0;
                if (diseaseId == 0)
                {
                    throw new InvalidOperationException($"Vaccine {completeDto.VaccineId} has no associated diseases");
                }

                var profileRepository = _unitOfWork.GetRepository<ChildVaccineProfile>();

                // 3. Tìm hoặc tạo bản ghi cho mũi hiện tại
                var currentProfile = await profileRepository.GetAsync(p =>
                    p.ChildId == completeDto.ChildId &&
                    p.VaccineId == completeDto.VaccineId &&
                    p.DoseNum == completeDto.DoseNumber);

                if (currentProfile == null)
                {
                    // Tạo mới nếu chưa có
                    currentProfile = new ChildVaccineProfile
                    {
                        ChildId = completeDto.ChildId,
                        VaccineId = completeDto.VaccineId,
                        DiseaseId = diseaseId,
                        DoseNum = completeDto.DoseNumber,
                        ExpectedDate = completeDto.ActualDate, // Set same as actual initially
                        ActualDate = completeDto.ActualDate,
                        Status = "Completed",
                        CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    };
                    await profileRepository.AddAsync(currentProfile);
                    await _unitOfWork.SaveChangesAsync(); // Save để có ID
                }

                // 4. Cập nhật bản ghi hiện tại thành "Completed"
                currentProfile.ActualDate = completeDto.ActualDate;
                currentProfile.Status = "Completed";
                currentProfile.Note = completeDto.Note ?? currentProfile.Note;
                currentProfile.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                profileRepository.Update(currentProfile);

                // 5. Check xem vaccine có bao nhiêu mũi và đã tiêm được mũi nào chưa
                var totalDoses = vaccine.NumberOfDoses; // Tổng số mũi của vaccine
                var nextDoseNumber = completeDto.DoseNumber + 1;

                _logger.LogInformation("Vaccine {VaccineId} has {TotalDoses} doses, current dose: {CurrentDose}, next dose: {NextDose}", 
                    completeDto.VaccineId, totalDoses, completeDto.DoseNumber, nextDoseNumber);

                ChildVaccineProfile? nextProfile = null;
                DateOnly? nextExpectedDate = null;

                // 6. Nếu còn mũi tiếp theo, tạo bản ghi cho mũi đó
                if (nextDoseNumber <= totalDoses)
                {
                    // Check xem đã có bản ghi cho mũi tiếp theo chưa
                    var existingNextProfile = await profileRepository.GetAsync(p =>
                        p.ChildId == completeDto.ChildId &&
                        p.VaccineId == completeDto.VaccineId &&
                        p.DoseNum == nextDoseNumber);

                    if (existingNextProfile == null)
                    {
                        // Tính toán ngày hẹn tiếp theo (dựa trên MinIntervalBetweenDoses của vaccine)
                        var intervalDays = vaccine.MinIntervalBetweenDoses > 0 ? vaccine.MinIntervalBetweenDoses : 28;
                        nextExpectedDate = completeDto.ActualDate.AddDays(intervalDays);

                        nextProfile = new ChildVaccineProfile
                        {
                            ChildId = completeDto.ChildId,
                            VaccineId = completeDto.VaccineId,
                            DiseaseId = diseaseId,
                            DoseNum = nextDoseNumber,
                            ExpectedDate = nextExpectedDate.Value,
                            ActualDate = DateOnly.MinValue, // Giá trị mặc định vì không nullable
                            Status = "Scheduled", // Đã lên lịch
                            Note = $"Mũi tiếp theo sau mũi {completeDto.DoseNumber}",
                            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                            UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                        };

                        await profileRepository.AddAsync(nextProfile);
                        
                        _logger.LogInformation("Created next dose profile for Child {ChildId}, Vaccine {VaccineId}, Dose {NextDose}, Expected date: {ExpectedDate}", 
                            completeDto.ChildId, completeDto.VaccineId, nextDoseNumber, nextExpectedDate);
                    }
                    else
                    {
                        nextProfile = existingNextProfile;
                        nextExpectedDate = existingNextProfile.ExpectedDate;
                        _logger.LogInformation("Next dose profile already exists for Child {ChildId}, Vaccine {VaccineId}, Dose {NextDose}", 
                            completeDto.ChildId, completeDto.VaccineId, nextDoseNumber);
                    }
                }
                else
                {
                    _logger.LogInformation("Vaccine {VaccineId} course completed for Child {ChildId}. No more doses needed.", 
                        completeDto.VaccineId, completeDto.ChildId);
                }

                // 7. Cập nhật appointment status thành "Completed"
                appointment.Status = "Completed";
                appointment.UpdatedAt = DateTime.UtcNow;
                var appointmentRepository = _unitOfWork.GetRepository<VaccinationAppointment>();
                appointmentRepository.Update(appointment);

                await _unitOfWork.SaveChangesAsync();

                // 8. Đếm số mũi đã hoàn thành
                var completedProfiles = await profileRepository.FindAsync(p =>
                    p.ChildId == completeDto.ChildId &&
                    p.VaccineId == completeDto.VaccineId &&
                    p.Status == "Completed");

                var completedDoses = completedProfiles.Count();
                var isVaccineCourseCompleted = completedDoses >= totalDoses;

                // 9. Load full data để return
                var completedProfile = await profileRepository.GetAsync(
                    p => p.VaccineProfileId == currentProfile.VaccineProfileId,
                    includeProperties: "Child,Vaccine"
                );

                ChildVaccineProfile? nextProfileWithData = null;
                if (nextProfile != null)
                {
                    nextProfileWithData = await profileRepository.GetAsync(
                        p => p.VaccineProfileId == nextProfile.VaccineProfileId,
                        includeProperties: "Child,Vaccine"
                    );
                }

                _logger.LogInformation("Successfully completed vaccination for Child {ChildId}, Vaccine {VaccineId}, Dose {DoseNumber}", 
                    completeDto.ChildId, completeDto.VaccineId, completeDto.DoseNumber);

                return new VaccinationCompletionResponseDTO
                {
                    CompletedDose = _mapper.Map<ChildVaccineProfileDTO>(completedProfile),
                    NextDose = nextProfileWithData != null ? _mapper.Map<ChildVaccineProfileDTO>(nextProfileWithData) : null,
                    HasNextDose = nextProfile != null,
                    IsVaccineCourseCompleted = isVaccineCourseCompleted,
                    TotalDoses = totalDoses,
                    CompletedDoses = completedDoses,
                    NextExpectedDate = nextExpectedDate,
                    Message = isVaccineCourseCompleted ? 
                        $"Hoàn thành toàn bộ liệu trình vaccine {vaccine.Name}" :
                        $"Hoàn thành mũi {completeDto.DoseNumber}/{totalDoses}. Mũi tiếp theo dự kiến: {nextExpectedDate?.ToString("dd/MM/yyyy")}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing vaccination for Child {ChildId}, Vaccine {VaccineId}, Dose {DoseNumber}", 
                    completeDto.ChildId, completeDto.VaccineId, completeDto.DoseNumber);
                throw;
            }
        }
    }
}