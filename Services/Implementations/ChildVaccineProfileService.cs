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
using Newtonsoft.Json;
using System.Linq.Expressions;

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
            // Bọc toàn bộ quá trình trong transaction để đảm bảo tính toàn vẹn dữ liệu
            Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null;
            try
            {
                transaction = await _unitOfWork.BeginTransactionAsync();
                _logger.LogInformation("Doctor completing vaccination for Appointment {AppointmentId}, FacilityVaccine {FacilityVaccineId}, Dose {DoseNumber}", 
                    completeDto.AppointmentId, completeDto.FacilityVaccineId, completeDto.DoseNumber);

                // 1. Validate appointment có status "Paid" và lấy ChildId từ appointment
                var appointmentRepo = _unitOfWork.GetRepository<VaccinationAppointment>();
                var appointment = await appointmentRepo.GetAsync(
                    a => a.AppointmentId == completeDto.AppointmentId,
                    "Child,Order,Order.OrderDetails");
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

                // ✅ Xác định đúng DiseaseId theo hồ sơ đã đặt và chuẩn hóa hồ sơ hiện tại cần cập nhật
                var profileRepository = _unitOfWork.GetRepository<ChildVaccineProfile>();
                int? determinedDiseaseId = null;
                ChildVaccineProfile? profileByAppointment = await profileRepository.GetAsync(p =>
                    p.AppointmentId == completeDto.AppointmentId &&
                    p.VaccineId == facilityVaccine.VaccineId);

                if (profileByAppointment != null)
                {
                    // Bắt buộc DoseNumber phải khớp với hồ sơ đã đặt để tránh tạo trùng hồ sơ
                    if (profileByAppointment.DoseNum != completeDto.DoseNumber)
                    {
                        throw new InvalidOperationException($"DoseNumber ({completeDto.DoseNumber}) không khớp với hồ sơ đã đặt (Dose {profileByAppointment.DoseNum}) cho appointment {completeDto.AppointmentId}");
                    }
                    determinedDiseaseId = profileByAppointment.DiseaseId;
                    _logger.LogInformation("Determined DiseaseId {DiseaseId} from booked profile by appointment {ProfileId}", determinedDiseaseId, profileByAppointment.VaccineProfileId);
                }
                else
                {
                    // Fallback: lấy theo đúng appointment + vaccine + dose (hành vi cũ)
                    var bookedProfile = await profileRepository.GetAsync(p =>
                        p.AppointmentId == completeDto.AppointmentId &&
                        p.VaccineId == facilityVaccine.VaccineId &&
                        p.DoseNum == completeDto.DoseNumber);
                    if (bookedProfile != null)
                    {
                        determinedDiseaseId = bookedProfile.DiseaseId;
                        _logger.LogInformation("Determined DiseaseId {DiseaseId} from booked ChildVaccineProfile {ProfileId}", determinedDiseaseId, bookedProfile.VaccineProfileId);
                    }
                }

                // 2) Nếu không có profile ở bước (1), thử lấy từ OrderDetails theo FacilityVaccineId
                if (determinedDiseaseId == null && appointment.OrderId.HasValue)
                {
                    var order = appointment.Order;
                    var detailFromOrder = order?.OrderDetails?.FirstOrDefault(od => od.FacilityVaccineId == completeDto.FacilityVaccineId);
                    if (detailFromOrder != null)
                    {
                        determinedDiseaseId = detailFromOrder.DiseaseId;
                        _logger.LogInformation("Determined DiseaseId {DiseaseId} from OrderDetail {OrderDetailId}", determinedDiseaseId, detailFromOrder.OrderDetailId);
                    }
                }

                // 3) IMPROVED FALLBACK: Tìm disease từ appointment booking context thay vì lấy ngẫu nhiên
                if (determinedDiseaseId == null)
                {
                    _logger.LogWarning("Using improved fallback logic to determine DiseaseId for Vaccine {VaccineId}", vaccine.VaccineId);
                    
                    // Cách 3a: Tìm từ tất cả ChildVaccineProfile có AppointmentId này (bất kể vaccine)
                    var allAppointmentProfiles = await profileRepository.FindAsync(p => p.AppointmentId == completeDto.AppointmentId);
                    if (allAppointmentProfiles.Any())
                    {
                        // Ưu tiên disease có CVP với vaccine đang complete, hoặc lấy disease đầu tiên từ appointment
                        var matchingVaccineProfile = allAppointmentProfiles.FirstOrDefault(p => p.VaccineId == facilityVaccine.VaccineId);
                        if (matchingVaccineProfile != null)
                        {
                            determinedDiseaseId = matchingVaccineProfile.DiseaseId;
                            _logger.LogInformation("Determined DiseaseId {DiseaseId} from ChildVaccineProfile of same vaccine in appointment", determinedDiseaseId);
                        }
                        else
                        {
                            // Lấy disease đầu tiên từ appointment (thường là disease chính đã book)
                            var firstAppointmentProfile = allAppointmentProfiles.First();
                            
                            // Kiểm tra vaccine hiện tại có chữa được disease này không
                            var canTreatThisDisease = vaccine.VaccineDiseases?.Any(vd => vd.DiseaseId == firstAppointmentProfile.DiseaseId) ?? false;
                            if (canTreatThisDisease)
                            {
                                determinedDiseaseId = firstAppointmentProfile.DiseaseId;
                                _logger.LogInformation("Determined DiseaseId {DiseaseId} from first disease in appointment (vaccine can treat this disease)", determinedDiseaseId);
                            }
                        }
                    }
                    
                    // Cách 3b: Nếu vẫn không có, tìm từ Order context
                    if (determinedDiseaseId == null && appointment.OrderId.HasValue)
                    {
                        _logger.LogInformation("Trying to determine disease from Order context for Order {OrderId}", appointment.OrderId.Value);
                        var order = appointment.Order;
                        if (order?.OrderDetails != null && order.OrderDetails.Any())
                        {
                            // Tìm OrderDetail có vaccine tương thích với vaccine đang complete
                            var compatibleOrderDetail = order.OrderDetails
                                .FirstOrDefault(od => od.FacilityVaccine?.VaccineId == facilityVaccine.VaccineId);
                            
                            if (compatibleOrderDetail != null)
                            {
                                determinedDiseaseId = compatibleOrderDetail.DiseaseId;
                                _logger.LogInformation("Determined DiseaseId {DiseaseId} from compatible OrderDetail {OrderDetailId}", determinedDiseaseId, compatibleOrderDetail.OrderDetailId);
                            }
                            else
                            {
                                // Lấy disease đầu tiên từ order và kiểm tra compatibility
                                var firstOrderDetail = order.OrderDetails.First();
                                var canTreatOrderDisease = vaccine.VaccineDiseases?.Any(vd => vd.DiseaseId == firstOrderDetail.DiseaseId) ?? false;
                                if (canTreatOrderDisease)
                                {
                                    determinedDiseaseId = firstOrderDetail.DiseaseId;
                                    _logger.LogInformation("Determined DiseaseId {DiseaseId} from first disease in order (vaccine can treat this disease)", determinedDiseaseId);
                                }
                            }
                        }
                    }
                    
                    // Cách 3c: Last resort - lấy disease đầu tiên của vaccine (logic cũ)
                    if (determinedDiseaseId == null)
                    {
                        _logger.LogWarning("Last resort: Fallback to first VaccineDisease for Vaccine {VaccineId}", vaccine.VaccineId);
                        if (vaccine.VaccineDiseases == null || !vaccine.VaccineDiseases.Any())
                        {
                            _logger.LogError("Vaccine {VaccineId} (Name: {VaccineName}) has no VaccineDiseases. Cannot determine disease for completion.", 
                                vaccine.VaccineId, vaccine.Name);
                            throw new InvalidOperationException($"Vaccine {vaccine.Name} (ID: {vaccine.VaccineId}) has no associated diseases. Cannot complete vaccination.");
                        }
                        determinedDiseaseId = vaccine.VaccineDiseases.First().DiseaseId;
                        _logger.LogWarning("Used first VaccineDisease {DiseaseId} as last resort for Vaccine {VaccineId}", determinedDiseaseId, vaccine.VaccineId);
                    }
                }

                // ✅ Double-check Disease exists in database
                var diseaseRepository = _unitOfWork.GetRepository<Disease>();
                var disease = await diseaseRepository.GetAsync(d => d.DiseaseId == determinedDiseaseId.Value);
                if (disease == null)
                {
                    _logger.LogError("Disease with ID {DiseaseId} not found in database", determinedDiseaseId);
                    throw new InvalidOperationException($"Disease with ID {determinedDiseaseId} not found in database. Data integrity issue.");
                }
                _logger.LogInformation("Using DiseaseId {DiseaseId} (Name: {DiseaseName}) for completion", disease.DiseaseId, disease.Name);

                // ✅ ActualDate tự động = ngày hôm nay
                var actualDate = DateOnly.FromDateTime(DateTime.Today);

                // 3. Tìm TẤT CẢ CVP đã có (được tạo từ Book) theo AppointmentId cho multi-disease vaccine
                var appointmentProfiles = await profileRepository.FindAsync(p => 
                    p.AppointmentId == completeDto.AppointmentId);

                if (!appointmentProfiles.Any())
                {
                    throw new InvalidOperationException("Không tìm thấy ChildVaccineProfile cho AppointmentId này. CVP phải được tạo từ booking trước.");
                }

                _logger.LogInformation("Tìm thấy {ProfileCount} ChildVaccineProfile cho AppointmentId {AppointmentId}", 
                    appointmentProfiles.Count(), completeDto.AppointmentId);

                // 4. ✅ CRITICAL FIX: CHỈ cập nhật PROFILE CHÍNH, không tạo thêm profiles mới
                ChildVaccineProfile? primaryProfile = null;
                
                // Lấy danh sách disease mà vaccine mới có thể chữa
                var newVaccineDiseaseIds = vaccine.VaccineDiseases?.Select(vd => vd.DiseaseId).ToList() ?? new List<int>();
                _logger.LogInformation("Vaccine {VaccineId} có thể chữa {DiseaseCount} diseases: [{DiseaseIds}]", 
                    facilityVaccine.VaccineId, newVaccineDiseaseIds.Count, string.Join(", ", newVaccineDiseaseIds));

                // ✅ PRIORITY 1: Tìm profile có DiseaseId khớp với determinedDiseaseId
                primaryProfile = appointmentProfiles.FirstOrDefault(p => p.DiseaseId == determinedDiseaseId.Value);
                
                // ✅ PRIORITY 2: Nếu không có, chọn profile đầu tiên và update DiseaseId
                if (primaryProfile == null)
                {
                    primaryProfile = appointmentProfiles.First();
                    _logger.LogInformation("Không tìm thấy profile với DiseaseId {DiseaseId}, sử dụng profile {ProfileId} và update DiseaseId", 
                        determinedDiseaseId.Value, primaryProfile.VaccineProfileId);
                }

                // ✅ CHỈ cập nhật primary profile, KHÔNG cập nhật tất cả profiles
                var oldVaccineId = primaryProfile.VaccineId;
                var oldDiseaseId = primaryProfile.DiseaseId;
                
                primaryProfile.ActualDate = actualDate;
                primaryProfile.Status = "Completed";
                primaryProfile.DoseNum = completeDto.DoseNumber;
                primaryProfile.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                primaryProfile.VaccineId = facilityVaccine.VaccineId;
                primaryProfile.DiseaseId = determinedDiseaseId.Value; // Luôn set theo determinedDiseaseId

                profileRepository.Update(primaryProfile);

                _logger.LogInformation("✅ Cập nhật PRIMARY ChildVaccineProfile {ProfileId}: VaccineId {OldVaccine}→{NewVaccine}, DiseaseId {OldDisease}→{NewDisease}, Status→Completed", 
                    primaryProfile.VaccineProfileId, oldVaccineId, facilityVaccine.VaccineId, oldDiseaseId, determinedDiseaseId.Value);

                // ✅ XÓA hoặc UPDATE các profiles khác thành CANCELLED để tránh duplicate
                var otherProfiles = appointmentProfiles.Where(p => p.VaccineProfileId != primaryProfile.VaccineProfileId);
                foreach (var otherProfile in otherProfiles)
                {
                    // Option 1: Cancel other profiles
                    otherProfile.Status = "Cancelled";
                    otherProfile.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    profileRepository.Update(otherProfile);
                    
                    _logger.LogInformation("🚫 Cancelled ChildVaccineProfile {ProfileId} (VaccineId {VaccineId}, DiseaseId {DiseaseId}) to avoid duplicate completion", 
                        otherProfile.VaccineProfileId, otherProfile.VaccineId, otherProfile.DiseaseId);
                }

                // ✅ Sử dụng primaryProfile làm currentProfile (đã được xác định ở trên)
                var currentProfile = primaryProfile;

                // 5. Check xem vaccine có bao nhiêu mũi và đã tiêm được mũi nào chưa
                var totalDoses = vaccine.NumberOfDoses; // Tổng số mũi của vaccine
                var nextDoseNumber = completeDto.DoseNumber + 1;

                _logger.LogInformation("Vaccine {VaccineId} has {TotalDoses} doses, current dose: {CurrentDose}, next dose: {NextDose}", 
                    facilityVaccine.VaccineId, totalDoses, completeDto.DoseNumber, nextDoseNumber);

                ChildVaccineProfile? nextProfile = null;
                DateOnly? nextExpectedDate = null;

                // 6. ✅ CRITICAL FIX: Chỉ tạo CVP mũi tiếp theo cho PRIMARY DISEASE (tránh duplicate)
                if (nextDoseNumber <= totalDoses)
                {
                    // ✅ CHỈ sử dụng ExpectedDateForNextDose khi thực sự có next dose
                    nextExpectedDate = completeDto.ExpectedDateForNextDose != default
                        ? completeDto.ExpectedDateForNextDose
                        : DateOnly.FromDateTime(DateTime.Today.AddDays(30));
                    
                    _logger.LogInformation("✅ Vaccine {VaccineId} chưa hoàn thành (Dose {CurrentDose}/{TotalDoses}), sẽ tạo next dose với ExpectedDate: {ExpectedDate}", 
                        facilityVaccine.VaccineId, completeDto.DoseNumber, totalDoses, nextExpectedDate);

                    // ✅ CHỈ tạo next dose profile cho PRIMARY DISEASE (determinedDiseaseId)
                    var primaryDiseaseId = determinedDiseaseId.Value;
                    _logger.LogInformation("🔄 Tạo next dose (Dose {NextDose}) CHỈ cho primary disease: {DiseaseId}", 
                        nextDoseNumber, primaryDiseaseId);

                    // Check xem đã có bản ghi cho mũi tiếp theo chưa
                    var existingNextProfile = await profileRepository.GetAsync(p =>
                        p.ChildId == currentProfile.ChildId &&
                        p.VaccineId == currentProfile.VaccineId &&
                        p.DiseaseId == primaryDiseaseId &&
                        p.DoseNum == nextDoseNumber);

                    if (existingNextProfile == null)
                    {
                        var newNextProfile = new ChildVaccineProfile
                        {
                            ChildId = currentProfile.ChildId,
                            VaccineId = currentProfile.VaccineId,  // Cùng vaccine
                            DiseaseId = primaryDiseaseId,  // Chỉ cho primary disease
                            AppointmentId = null, // ✅ NextDose chưa có appointment
                            DoseNum = nextDoseNumber,
                            ExpectedDate = nextExpectedDate.Value,
                            ActualDate = null, // ✅ NextDose chưa tiêm nên ActualDate = null
                            Status = "Scheduled", // Đã lên lịch
                            IsRequired = true,
                            Priority = "High",
                            Note = null, // ✅ NextDose chưa có note
                            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                            UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                        };

                        await profileRepository.AddAsync(newNextProfile);
                        nextProfile = newNextProfile;
                        
                        _logger.LogInformation("✅ Tạo next dose profile cho Child {ChildId}, Vaccine {VaccineId}, Disease {DiseaseId}, Dose {NextDose}", 
                            currentProfile.ChildId, currentProfile.VaccineId, primaryDiseaseId, nextDoseNumber);
                    }
                    else
                    {
                        nextProfile = existingNextProfile;
                        nextExpectedDate = existingNextProfile.ExpectedDate;
                        
                        _logger.LogInformation("Next dose profile đã tồn tại cho Child {ChildId}, Vaccine {VaccineId}, Disease {DiseaseId}, Dose {NextDose}", 
                            currentProfile.ChildId, currentProfile.VaccineId, primaryDiseaseId, nextDoseNumber);
                    }
                }
                else
                {
                    _logger.LogInformation("🏁 Vaccine {VaccineId} course completed for Child {ChildId} (Dose {CurrentDose}/{TotalDoses}). No more doses needed.", 
                        currentProfile.VaccineId, currentProfile.ChildId, completeDto.DoseNumber, totalDoses);
                    
                    // ✅ XỬ LÝ: Khi vaccine hiện tại hoàn thành, tạo profile cho bệnh kế tiếp trong order
                    if (completeDto.ExpectedDateForNextDose != default && appointment.OrderId.HasValue)
                    {
                        _logger.LogInformation("🔍 Vaccine {VaccineId} đã hoàn thành, tìm bệnh kế tiếp trong Order {OrderId} để sử dụng ExpectedDate {ExpectedDate}", 
                            facilityVaccine.VaccineId, appointment.OrderId.Value, completeDto.ExpectedDateForNextDose);
                        
                        // Tìm các bệnh khác trong order mà chưa có appointment
                        var orderRepo = _unitOfWork.GetRepository<Order>();
                        var order = await orderRepo.GetAsync(o => o.OrderId == appointment.OrderId.Value, "OrderDetails,OrderDetails.Disease");
                        
                        if (order?.OrderDetails != null)
                        {
                            // Lấy tất cả diseases trong order
                            var orderDiseaseIds = order.OrderDetails.Select(od => od.DiseaseId).Distinct().ToList();
                            
                            // Loại bỏ disease đã hoàn thành (determinedDiseaseId)
                            var remainingDiseaseIds = orderDiseaseIds.Where(id => id != determinedDiseaseId.Value).ToList();
                            
                            if (remainingDiseaseIds.Any())
                            {
                                _logger.LogInformation("📋 Tìm thấy {Count} bệnh còn lại trong order: [{DiseaseIds}]", 
                                    remainingDiseaseIds.Count, string.Join(", ", remainingDiseaseIds));
                                
                                // ✅ PRIORITY 1: Sử dụng NextVaccineId từ request nếu có
                                int selectedNextVaccineId;
                                int selectedNextDiseaseId;
                                
                                if (completeDto.NextVaccineId.HasValue)
                                {
                                    // Validate vaccine có trong order không
                                    var validOrderDetail = order.OrderDetails?.FirstOrDefault(od => 
                                        od.FacilityVaccine?.VaccineId == completeDto.NextVaccineId.Value && 
                                        remainingDiseaseIds.Contains(od.DiseaseId));
                                    
                                    if (validOrderDetail != null)
                                    {
                                        selectedNextVaccineId = completeDto.NextVaccineId.Value;
                                        selectedNextDiseaseId = validOrderDetail.DiseaseId;
                                        _logger.LogInformation("✅ Sử dụng NextVaccineId từ request: VaccineId {VaccineId} → DiseaseId {DiseaseId}", 
                                            selectedNextVaccineId, selectedNextDiseaseId);
                                    }
                                    else
                                    {
                                        // Fallback nếu vaccine không hợp lệ
                                        var fallbackOrderDetail = order.OrderDetails?.FirstOrDefault(od => remainingDiseaseIds.Contains(od.DiseaseId));
                                        selectedNextVaccineId = fallbackOrderDetail?.FacilityVaccine?.VaccineId ?? 0;
                                        selectedNextDiseaseId = fallbackOrderDetail?.DiseaseId ?? remainingDiseaseIds.First();
                                        
                                        _logger.LogWarning("⚠️ NextVaccineId {RequestedVaccineId} không có trong order hoặc đã được xử lý, chọn VaccineId {SelectedVaccineId} → DiseaseId {SelectedDiseaseId} thay thế", 
                                            completeDto.NextVaccineId.Value, selectedNextVaccineId, selectedNextDiseaseId);
                                    }
                                }
                                else
                                {
                                    // ✅ PRIORITY 2: Fallback chọn vaccine/disease đầu tiên trong danh sách còn lại
                                    var fallbackOrderDetail = order.OrderDetails?.FirstOrDefault(od => remainingDiseaseIds.Contains(od.DiseaseId));
                                    selectedNextVaccineId = fallbackOrderDetail?.FacilityVaccine?.VaccineId ?? 0;
                                    selectedNextDiseaseId = fallbackOrderDetail?.DiseaseId ?? remainingDiseaseIds.First();
                                    _logger.LogInformation("🔄 Tự động chọn vaccine/disease kế tiếp: VaccineId {VaccineId} → DiseaseId {DiseaseId}", 
                                        selectedNextVaccineId, selectedNextDiseaseId);
                                }
                                
                                // ✅ TẠO PROFILE CHO VACCINE/DISEASE KẾ TIẾP
                                await CreateNextVaccineProfileAsync(currentProfile.ChildId, selectedNextVaccineId, selectedNextDiseaseId, completeDto.ExpectedDateForNextDose, order);
                            }
                            else
                            {
                                _logger.LogInformation("✅ Tất cả bệnh trong order đã được xử lý. ExpectedDate {ExpectedDate} sẽ không được sử dụng.", 
                                    completeDto.ExpectedDateForNextDose);
                            }
                        }
                    }
                    else if (completeDto.ExpectedDateForNextDose != default)
                    {
                        _logger.LogWarning("⚠️ ExpectedDateForNextDose ({ExpectedDate}) được nhập nhưng sẽ KHÔNG được sử dụng vì vaccine {VaccineId} đã hoàn thành và không có order", 
                            completeDto.ExpectedDateForNextDose, facilityVaccine.VaccineId);
                    }
                }

                // 7. Cập nhật appointment: status và note
                appointment.Status = "Completed";
                appointment.Note = completeDto.Note ?? appointment.Note;
                appointment.UpdatedAt = DateTime.UtcNow;
                var appointmentRepository = _unitOfWork.GetRepository<VaccinationAppointment>();
                appointmentRepository.Update(appointment);

                // ✅ Trừ RemainingQuantity khi bác sĩ complete vaccination
                if (appointment.OrderId.HasValue)
                {
                    var orderRepo = _unitOfWork.GetRepository<Order>();
                    var order = await orderRepo.GetAsync(o => o.OrderId == appointment.OrderId.Value, "OrderDetails");
                    
                    if (order != null)
                    {
                        var orderDetailRepo = _unitOfWork.GetRepository<OrderDetail>();
                        var relevantOrderDetail = order.OrderDetails
                            .FirstOrDefault(od => od.FacilityVaccineId == completeDto.FacilityVaccineId && od.DiseaseId == determinedDiseaseId.Value);
                        
                        if (relevantOrderDetail == null)
                        {
                            _logger.LogWarning("Không tìm thấy OrderDetail phù hợp cho appointment {AppointmentId}. Không thể hoàn tất vì gói không khớp bệnh/vaccine.", appointment.AppointmentId);
                            throw new InvalidOperationException("Gói không có dòng phù hợp hoặc đã hết số lượng cho bệnh/vaccine này.");
                        }

                        if (relevantOrderDetail.RemainingQuantity <= 0)
                        {
                            _logger.LogWarning("OrderDetail {OrderDetailId} đã hết RemainingQuantity. Không thể hoàn tất.", relevantOrderDetail.OrderDetailId);
                            throw new InvalidOperationException("Gói đã hết số lượng cho bệnh/vaccine này.");
                        }

                        var oldQuantity = relevantOrderDetail.RemainingQuantity;
                        relevantOrderDetail.RemainingQuantity -= 1;
                        relevantOrderDetail.UpdatedAt = DateTime.UtcNow;
                        orderDetailRepo.Update(relevantOrderDetail);
                        
                        _logger.LogInformation("Đã trừ 1 vaccine từ OrderDetail {OrderDetailId} khi complete vaccination. Từ {OldQuantity} xuống {NewQuantity}", 
                            relevantOrderDetail.OrderDetailId, oldQuantity, relevantOrderDetail.RemainingQuantity);
                    }
                }
                else
                {
                    _logger.LogInformation("Appointment {AppointmentId} không có OrderId, tiến hành trừ kho FacilityVaccine.AvailableQuantity", appointment.AppointmentId);
                    // Giảm tồn kho thực tế của cơ sở khi không dùng gói
                    var facilityVaccineRepoForReduce = _unitOfWork.GetRepository<FacilityVaccine>();
                    var facilityVaccineForReduce = await facilityVaccineRepoForReduce.GetAsync(fv => fv.FacilityVaccineId == completeDto.FacilityVaccineId);
                    if (facilityVaccineForReduce == null)
                    {
                        throw new KeyNotFoundException($"FacilityVaccine {completeDto.FacilityVaccineId} not found for stock deduction");
                    }
                    if (facilityVaccineForReduce.AvailableQuantity <= 0)
                    {
                        throw new InvalidOperationException("Kho cơ sở không đủ số lượng để hoàn tất tiêm");
                    }
                    facilityVaccineForReduce.AvailableQuantity -= 1;
                    facilityVaccineForReduce.UpdatedAt = DateTime.UtcNow;
                    facilityVaccineRepoForReduce.Update(facilityVaccineForReduce);
                }

                await _unitOfWork.SaveChangesAsync();

                // 9. Đếm số mũi đã hoàn thành
                var completedProfiles = await profileRepository.FindAsync(p =>
                    p.ChildId == currentProfile.ChildId &&
                    p.VaccineId == currentProfile.VaccineId &&
                    p.DiseaseId == currentProfile.DiseaseId &&
                    p.Status == "Completed");

                var completedDoses = completedProfiles.Count();
                var isVaccineCourseCompleted = completedDoses >= totalDoses;

                // 10. Load full data để return
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
                    currentProfile.ChildId, completeDto.FacilityVaccineId, completeDto.DoseNumber);

                var result = new VaccinationCompletionResponseDTO
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
                // Commit transaction cuối cùng
                await transaction.CommitAsync();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing vaccination for Appointment {AppointmentId}, FacilityVaccine {FacilityVaccineId}, Dose {DoseNumber}", 
                    completeDto.AppointmentId, completeDto.FacilityVaccineId, completeDto.DoseNumber);
                try { if (transaction != null) await transaction.RollbackAsync(); } catch { /* ignore */ }
                throw;
            }
        }

        private static (DateOnly? minDate, DateOnly? maxDate) ComputeExpectedDateBounds(DateOnly birthDate, IEnumerable<VaccineTemplate> templates)
        {
            DateOnly? min = null;
            DateOnly? max = null;
            foreach (var t in templates)
            {
                if (!string.IsNullOrWhiteSpace(t.PeriodFrom))
                {
                    var fromOffset = ParsePeriodToDays(t.PeriodFrom);
                    var candidate = DateOnly.FromDateTime(birthDate.ToDateTime(TimeOnly.MinValue).AddDays(fromOffset));
                    min = !min.HasValue || candidate < min.Value ? candidate : min;
                }
                if (!string.IsNullOrWhiteSpace(t.PeriodTo))
                {
                    var toOffset = ParsePeriodToDays(t.PeriodTo);
                    var candidate = DateOnly.FromDateTime(birthDate.ToDateTime(TimeOnly.MinValue).AddDays(toOffset));
                    max = !max.HasValue || candidate > max.Value ? candidate : max;
                }
            }
            return (min, max);
        }

        private static DateOnly ComputeNextExpectedDateFromTemplates(DateOnly birthDate, IEnumerable<VaccineTemplate> templates)
        {
            var (minDate, maxDate) = ComputeExpectedDateBounds(birthDate, templates);
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (minDate.HasValue && today < minDate.Value)
            {
                return minDate.Value;
            }
            if (maxDate.HasValue && today > maxDate.Value)
            {
                // Nếu đã trễ khung khuyến nghị, đặt ở biên trên để nhắc sớm
                return maxDate.Value;
            }
            // Nếu trong khoảng, dùng hôm nay
            return today;
        }

        private static int ParsePeriodToDays(string period)
        {
            // Hỗ trợ các hậu tố: d (ngày), w (tuần = 7 ngày), m (tháng ~ 30 ngày), y (năm ~ 365 ngày)
            period = period.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(period)) return 0;
            int factor = 1;
            if (period.EndsWith("d")) factor = 1;
            else if (period.EndsWith("w")) factor = 7;
            else if (period.EndsWith("m")) factor = 30;
            else if (period.EndsWith("y")) factor = 365;
            var numberPart = new string(period.TakeWhile(char.IsDigit).ToArray());
            if (int.TryParse(numberPart, out var n))
            {
                return n * factor;
            }
            return 0;
        }
        public async Task<IEnumerable<VaccineRecordDTO>> GetVaccineRecordAsync(int childId, int? diseaseId = null)
        {
            try
            {
                // Get child information
                var childRepository = _unitOfWork.GetRepository<Child>();
                var child = await childRepository.GetAsync(c => c.ChildId == childId);
                if (child == null)
                {
                    _logger.LogWarning($"Child with ID {childId} not found");
                    throw new KeyNotFoundException($"Child with ID {childId} not found");
                }

                // Get all vaccine profiles for the child with optional diseaseId filter
                var vaccineProfileRepository = _unitOfWork.GetRepository<ChildVaccineProfile>();
                Expression<Func<ChildVaccineProfile, bool>> filter = cvp => cvp.ChildId == childId && cvp.Status == "Completed";
                if (diseaseId.HasValue)
                {
                    filter = cvp => cvp.ChildId == childId && cvp.Status == "Completed" && cvp.DiseaseId == diseaseId.Value;
                }

                var childVaccineProfilesResult = await vaccineProfileRepository.GetAllAsync(
                    filter: filter,
                    include: "Disease"
                );
                var childVaccineProfiles = childVaccineProfilesResult.Data ?? new List<ChildVaccineProfile>();
                _logger.LogInformation($"Retrieved {childVaccineProfiles.Count} completed vaccine profiles for ChildId {childId}");

                // Get all vaccine templates
                var vaccineTemplateRepository = _unitOfWork.GetRepository<VaccineTemplate>();
                var vaccineTemplatesResult = await vaccineTemplateRepository.GetAllAsync(
                    include: "Disease"
                );
                var vaccineTemplates = vaccineTemplatesResult.Data ?? new List<VaccineTemplate>();
                _logger.LogInformation($"Retrieved {vaccineTemplates.Count} vaccine templates");

                // Group child vaccine profiles by DiseaseId and count completed doses
                var completedDosesByDisease = childVaccineProfiles
                    .GroupBy(cvp => cvp.DiseaseId)
                    .Select(g => new
                    {
                        DiseaseId = g.Key,
                        DiseaseName = g.First().Disease?.Name ?? "Unknown",
                        CompletedDoseNum = g.Count()
                    })
                    .ToList();

                // Build vaccine record for all vaccine templates or filtered by diseaseId
                var vaccineRecords = new List<VaccineRecordDTO>();
                var relevantTemplates = diseaseId.HasValue ? vaccineTemplates.Where(t => t.DiseaseId == diseaseId.Value) : vaccineTemplates;

                foreach (var template in relevantTemplates)
                {
                    var completedProfile = completedDosesByDisease.FirstOrDefault(c => c.DiseaseId == template.DiseaseId);
                    int completedDoseNum = completedProfile?.CompletedDoseNum ?? 0;
                    string status = completedDoseNum >= template.DoseNum ? "Đã đủ liều" :
                                    completedDoseNum == 0 ? "Chưa tiêm" : "Chưa đủ liều";

                    vaccineRecords.Add(new VaccineRecordDTO
                    {
                        DiseaseId = template.DiseaseId,
                        DiseaseName = template.Disease?.Name ?? "Unknown",
                        RequiredDoseNum = template.DoseNum,
                        CompletedDoseNum = completedDoseNum,
                        IsRequired = template.IsRequired,
                        Status = status,
                        PeriodFrom = template.PeriodFrom,
                        PeriodTo = template.PeriodTo
                    });
                }

                // Log result
                var settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };
                return vaccineRecords;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving vaccine record for ChildId {childId} with DiseaseId {diseaseId}");
                throw;
            }
        }
        
        /// <summary>
        /// Tạo ChildVaccineProfile cho vaccine/bệnh kế tiếp trong order khi vaccine hiện tại đã hoàn thành
        /// </summary>
        private async Task CreateNextVaccineProfileAsync(int childId, int nextVaccineId, int nextDiseaseId, DateOnly expectedDate, Order order)
        {
            try
            {
                _logger.LogInformation("🎯 Tạo profile cho vaccine/bệnh kế tiếp - ChildId: {ChildId}, VaccineId: {VaccineId}, DiseaseId: {DiseaseId}, ExpectedDate: {ExpectedDate}", 
                    childId, nextVaccineId, nextDiseaseId, expectedDate);

                // Validate vaccine và disease có trong order không
                var orderDetail = order.OrderDetails?.FirstOrDefault(od => 
                    od.FacilityVaccine?.VaccineId == nextVaccineId && od.DiseaseId == nextDiseaseId);
                
                if (orderDetail?.FacilityVaccine == null)
                {
                    _logger.LogWarning("⚠️ Không tìm thấy vaccine {VaccineId} cho disease {DiseaseId} trong order {OrderId}", 
                        nextVaccineId, nextDiseaseId, order.OrderId);
                    return;
                }

                var vaccineId = nextVaccineId;
                
                // Kiểm tra xem đã có profile cho vaccine/disease này chưa
                var profileRepository = _unitOfWork.GetRepository<ChildVaccineProfile>();
                var existingProfile = await profileRepository.GetAsync(p => 
                    p.ChildId == childId && 
                    p.DiseaseId == nextDiseaseId && 
                    p.VaccineId == vaccineId &&
                    p.DoseNum == 1 &&
                    (p.Status == "Scheduled" || p.Status == "Pending"));

                if (existingProfile != null)
                {
                    _logger.LogInformation("✅ Profile đã tồn tại cho vaccine {VaccineId}/disease {DiseaseId}, cập nhật ExpectedDate từ {OldDate} thành {NewDate}", 
                        vaccineId, nextDiseaseId, existingProfile.ExpectedDate, expectedDate);
                    
                    // Cập nhật ExpectedDate nếu profile đã tồn tại
                    existingProfile.ExpectedDate = expectedDate;
                    existingProfile.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    profileRepository.Update(existingProfile);
                }
                else
                {
                    // Tạo profile mới
                    var newProfile = new ChildVaccineProfile
                    {
                        ChildId = childId,
                        VaccineId = vaccineId,
                        DiseaseId = nextDiseaseId,
                        AppointmentId = null, // Chưa có appointment
                        DoseNum = 1, // Mũi đầu tiên cho disease mới
                        ExpectedDate = expectedDate,
                        ActualDate = null,
                        Status = "Scheduled",
                        IsRequired = true,
                        Priority = "High",
                        Note = "Tạo tự động sau khi hoàn thành vaccine trước đó",
                        CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    };

                    await profileRepository.AddAsync(newProfile);
                    
                    _logger.LogInformation("✅ Tạo thành công profile cho vaccine/bệnh kế tiếp - ProfileId: {ProfileId}, VaccineId: {VaccineId}, DiseaseId: {DiseaseId}", 
                        newProfile.VaccineProfileId, vaccineId, nextDiseaseId);
                }

                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi tạo profile cho vaccine/bệnh kế tiếp - ChildId: {ChildId}, VaccineId: {VaccineId}, DiseaseId: {DiseaseId}", 
                    childId, nextVaccineId, nextDiseaseId);
                throw;
            }
        }
    }
}