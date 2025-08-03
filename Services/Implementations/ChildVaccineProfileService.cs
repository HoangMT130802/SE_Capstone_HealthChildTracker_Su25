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
                var profiles = await profileRepository.FindAsync(r => r.ChildId == childId, includeProperties: "Child,Vaccine,Appointment,Appointment.Schedule,Appointment.Schedule.Facility");
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
                var profiles = await profileRepository.FindAsync(r => r.ChildId == childId, includeProperties: "Child,Vaccine,Appointment,Appointment.Schedule,Appointment.Schedule.Facility");
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
        public async Task<VaccinationCompletionResponseDTO> CompleteVaccinationAsync(CompleteVaccinationDTO completeDto, int currentUserId)
        {
            try
            {
                _logger.LogInformation("Doctor completing vaccination for Appointment {AppointmentId}, FacilityVaccine {FacilityVaccineId}, Dose {DoseNumber}", 
                    completeDto.AppointmentId, completeDto.FacilityVaccineId, completeDto.DoseNumber);

                // 1. Validate appointment có status "Paid" và lấy ChildId từ appointment
                var appointmentRepo = _unitOfWork.GetRepository<VaccinationAppointment>();
                var appointment = await appointmentRepo.GetAsync(a => a.AppointmentId == completeDto.AppointmentId, "Child");
                if (appointment == null || appointment.Status != "Paid")
                {
                    throw new InvalidOperationException($"Appointment {completeDto.AppointmentId} not found or not in Paid status");
                }

                // ✅ Lấy ChildId từ appointment, không từ DTO
                var childId = appointment.ChildId;
                var child = appointment.Child;

                if (child == null)
                {
                    var childRepository = _unitOfWork.GetRepository<Child>();
                    child = await childRepository.GetAsync(c => c.ChildId == childId);
                    if (child == null)
                    {
                        throw new KeyNotFoundException($"Child with ID {childId} not found");
                    }
                }

                // 2. Validate và lấy thông tin từ FacilityVaccine
                var facilityVaccineRepo = _unitOfWork.GetRepository<FacilityVaccine>();
                var accountRepo = _unitOfWork.GetRepository<Account>();
                var facilityStaffRepo = _unitOfWork.GetRepository<FacilityStaff>();

                // Lấy thông tin account và facility của doctor
                var account = await accountRepo.GetAsync(a => a.AccountId == currentUserId);
                if (account == null || account.Role != "FacilityStaff")
                {
                    throw new InvalidOperationException("Only facility staff can complete vaccination");
                }

                var facilityStaff = await facilityStaffRepo.GetAsync(fs => fs.AccountId == currentUserId);
                if (facilityStaff == null)
                {
                    throw new InvalidOperationException("Facility staff information not found");
                }

                var facilityId = facilityStaff.FacilityId;

                // Validate FacilityVaccine thuộc về facility của doctor
                var facilityVaccine = await facilityVaccineRepo.GetAsync(fv => 
                    fv.FacilityVaccineId == completeDto.FacilityVaccineId && 
                    fv.FacilityId == facilityId, "Vaccine,Vaccine.VaccineDiseases,Vaccine.VaccineDiseases.Disease");

                if (facilityVaccine == null)
                {
                    throw new InvalidOperationException($"FacilityVaccine {completeDto.FacilityVaccineId} not found or doesn't belong to your facility");
                }

                var vaccine = facilityVaccine.Vaccine;
                if (vaccine == null)
                {
                    var vaccineRepository = _unitOfWork.GetRepository<Vaccine>();
                    vaccine = await vaccineRepository.GetAsync(v => v.VaccineId == facilityVaccine.VaccineId, "VaccineDiseases,VaccineDiseases.Disease");
                    if (vaccine == null)
                    {
                        throw new KeyNotFoundException($"Vaccine with ID {facilityVaccine.VaccineId} not found");
                    }
                }

                // ✅ Validate doseNumber <= NumberOfDoses của vaccine
                if (completeDto.DoseNumber < 1 || completeDto.DoseNumber > vaccine.NumberOfDoses)
                {
                    throw new InvalidOperationException($"Invalid dose number {completeDto.DoseNumber}. Vaccine {vaccine.Name} has {vaccine.NumberOfDoses} doses (range: 1-{vaccine.NumberOfDoses})");
                }

                // ✅ Validate và lấy DiseaseId với proper error handling
                _logger.LogInformation("Checking VaccineDiseases for Vaccine {VaccineId} (Name: {VaccineName})", vaccine.VaccineId, vaccine.Name);
                
                if (vaccine.VaccineDiseases == null || !vaccine.VaccineDiseases.Any())
                {
                    _logger.LogError("Vaccine {VaccineId} (Name: {VaccineName}) has no VaccineDiseases. VaccineDiseases is null: {IsNull}, Count: {Count}", 
                        vaccine.VaccineId, vaccine.Name, vaccine.VaccineDiseases == null, vaccine.VaccineDiseases?.Count ?? 0);
                    throw new InvalidOperationException($"Vaccine {vaccine.Name} (ID: {vaccine.VaccineId}) has no associated diseases. Cannot complete vaccination.");
                }

                var firstVaccineDisease = vaccine.VaccineDiseases.FirstOrDefault();
                if (firstVaccineDisease?.DiseaseId == null)
                {
                    _logger.LogError("Vaccine {VaccineId} has invalid disease association. FirstVaccineDisease is null: {IsNull}", 
                        vaccine.VaccineId, firstVaccineDisease == null);
                    throw new InvalidOperationException($"Vaccine {vaccine.Name} has invalid disease association");
                }

                var diseaseId = firstVaccineDisease.DiseaseId;
                _logger.LogInformation("Using DiseaseId {DiseaseId} from Vaccine {VaccineId}", diseaseId, vaccine.VaccineId);

                // ✅ Double-check Disease exists in database
                var diseaseRepository = _unitOfWork.GetRepository<Disease>();
                var disease = await diseaseRepository.GetAsync(d => d.DiseaseId == diseaseId);
                if (disease == null)
                {
                    _logger.LogError("Disease with ID {DiseaseId} not found in database", diseaseId);
                    throw new InvalidOperationException($"Disease with ID {diseaseId} not found in database. Data integrity issue.");
                }
                
                _logger.LogInformation("Successfully validated Disease {DiseaseId} (Name: {DiseaseName})", disease.DiseaseId, disease.Name);

                // ✅ ActualDate tự động = ngày hôm nay
                var actualDate = DateOnly.FromDateTime(DateTime.Today);

                var profileRepository = _unitOfWork.GetRepository<ChildVaccineProfile>();

                // 3. Tìm hoặc tạo bản ghi cho mũi hiện tại
                var currentProfile = await profileRepository.GetAsync(p =>
                    p.ChildId == childId &&
                    p.VaccineId == facilityVaccine.VaccineId &&
                    p.DoseNum == completeDto.DoseNumber);

                if (currentProfile == null)
                {
                    // Tạo mới nếu chưa có
                    currentProfile = new ChildVaccineProfile
                    {
                        ChildId = childId,
                        VaccineId = facilityVaccine.VaccineId,
                        DiseaseId = diseaseId,
                        AppointmentId = completeDto.AppointmentId,
                        DoseNum = completeDto.DoseNumber,
                        ExpectedDate = actualDate, // Set same as actual initially
                        ActualDate = actualDate,
                        Status = "Completed",
                        IsRequired = true, // Default value
                        Priority = "High", // Default value  
                        Note = completeDto.Note ?? "",
                        CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    };
                    await profileRepository.AddAsync(currentProfile);
                    await _unitOfWork.SaveChangesAsync(); // Save để có ID
                }
                else
                {
                    // 4. Cập nhật bản ghi hiện tại thành "Completed"
                    currentProfile.ActualDate = actualDate;
                    currentProfile.Status = "Completed";
                    currentProfile.Note = completeDto.Note ?? currentProfile.Note;
                    currentProfile.AppointmentId = completeDto.AppointmentId;
                    currentProfile.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                    profileRepository.Update(currentProfile);
                }

                // 5. Check xem vaccine có bao nhiêu mũi và đã tiêm được mũi nào chưa
                var totalDoses = vaccine.NumberOfDoses; // Tổng số mũi của vaccine
                var nextDoseNumber = completeDto.DoseNumber + 1;

                _logger.LogInformation("Vaccine {VaccineId} has {TotalDoses} doses, current dose: {CurrentDose}, next dose: {NextDose}", 
                    facilityVaccine.VaccineId, totalDoses, completeDto.DoseNumber, nextDoseNumber);

                ChildVaccineProfile? nextProfile = null;
                DateOnly? nextExpectedDate = null;

                // 6. Nếu còn mũi tiếp theo, tạo bản ghi cho mũi đó
                if (nextDoseNumber <= totalDoses)
                {
                    // Check xem đã có bản ghi cho mũi tiếp theo chưa
                    var existingNextProfile = await profileRepository.GetAsync(p =>
                        p.ChildId == childId &&
                        p.VaccineId == facilityVaccine.VaccineId &&
                        p.DoseNum == nextDoseNumber);

                    if (existingNextProfile == null)
                    {
                        // ✅ Sử dụng ExpectedDateForNextDose từ DTO thay vì tính toán tự động
                        nextExpectedDate = completeDto.ExpectedDateForNextDose;

                        nextProfile = new ChildVaccineProfile
                        {
                            ChildId = childId,
                            VaccineId = facilityVaccine.VaccineId,
                            DiseaseId = diseaseId,
                            AppointmentId = null, // ✅ NextDose chưa có appointment
                            DoseNum = nextDoseNumber,
                            ExpectedDate = nextExpectedDate.Value,
                            ActualDate = DateOnly.MinValue, // ✅ NextDose chưa tiêm (null trong entity)
                            Status = "Scheduled", // Đã lên lịch
                            IsRequired = true,
                            Priority = "High",
                            Note = null, // ✅ NextDose chưa có note
                            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                            UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                        };

                        await profileRepository.AddAsync(nextProfile);
                        
                        _logger.LogInformation("Created next dose profile for Child {ChildId}, Vaccine {VaccineId}, Dose {NextDose}, Expected date: {ExpectedDate}", 
                            childId, facilityVaccine.VaccineId, nextDoseNumber, nextExpectedDate);
                    }
                    else
                    {
                        nextProfile = existingNextProfile;
                        nextExpectedDate = existingNextProfile.ExpectedDate;
                        _logger.LogInformation("Next dose profile already exists for Child {ChildId}, Vaccine {VaccineId}, Dose {NextDose}", 
                            childId, facilityVaccine.VaccineId, nextDoseNumber);
                    }
                }
                else
                {
                    _logger.LogInformation("Vaccine {VaccineId} course completed for Child {ChildId}. No more doses needed.", 
                        facilityVaccine.VaccineId, childId);
                }

                // 7. Cập nhật appointment status thành "Completed"
                appointment.Status = "Completed";
                appointment.UpdatedAt = DateTime.UtcNow;
                var appointmentRepository = _unitOfWork.GetRepository<VaccinationAppointment>();
                appointmentRepository.Update(appointment);

                await _unitOfWork.SaveChangesAsync();

                // 8. Đếm số mũi đã hoàn thành
                var completedProfiles = await profileRepository.FindAsync(p =>
                    p.ChildId == childId &&
                    p.VaccineId == facilityVaccine.VaccineId &&
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

                _logger.LogInformation("Successfully completed vaccination for Child {ChildId}, FacilityVaccine {FacilityVaccineId}, Dose {DoseNumber}", 
                    childId, completeDto.FacilityVaccineId, completeDto.DoseNumber);

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
                _logger.LogError(ex, "Error completing vaccination for Appointment {AppointmentId}, FacilityVaccine {FacilityVaccineId}, Dose {DoseNumber}", 
                    completeDto.AppointmentId, completeDto.FacilityVaccineId, completeDto.DoseNumber);
                throw;
            }
        }
    }
}