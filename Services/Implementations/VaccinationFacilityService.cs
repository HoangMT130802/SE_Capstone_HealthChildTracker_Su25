using AutoMapper;
using Contracts.DTOs.VaccinationFacility;
using Repositories.Common;
using Repositories.Entities;
using Repositories.Interfaces;
using Repositories.Models.QueryModels;
using Services.Interfaces;

namespace Services.Implementations
{
    public class VaccinationFacilityService : IVaccinationFacilityService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public VaccinationFacilityService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<QueryResultModel<List<VaccinationFacilityDTO>>> GetAllFacilitiesAsync(int pageIndex = 1, int pageSize = 10)
        {
            try
            {
                var facilityRepository = _unitOfWork.GetRepository<VaccinationFacility>();
                var result = await facilityRepository.GetAllAsync(
                    filter: f => f.Status > 0,
                    orderBy: f => f.OrderByDescending(x => x.CreatedAt),
                    pageIndex: pageIndex,
                    pageSize: pageSize
                );

                var facilityDTOs = _mapper.Map<List<VaccinationFacilityDTO>>(result.Data);

                return new QueryResultModel<List<VaccinationFacilityDTO>>
                {
                    Data = facilityDTOs,
                    TotalCount = result.TotalCount
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách cơ sở: {ex.Message}");
            }
        }

        public async Task<VaccinationFacilityDTO?> GetFacilityByIdAsync(int facilityId)
        {
            try
            {
                var facilityRepository = _unitOfWork.GetRepository<VaccinationFacility>();
                var facility = await facilityRepository.GetAsync(f => f.FacilityId == facilityId && f.Status > 0);

                return facility != null ? _mapper.Map<VaccinationFacilityDTO>(facility) : null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy thông tin cơ sở: {ex.Message}");
            }
        }

        public async Task<VaccinationFacilityDTO?> GetFacilityByManagerIdAsync(int accountId)
        {
            try
            {
                var facilityStaffRepository = _unitOfWork.GetRepository<FacilityStaff>();
                var facilityStaff = await facilityStaffRepository.GetAsync(
                    fs => fs.AccountId == accountId && fs.Status && fs.FacilityId > 0,
                    "Facility"
                );

                if (facilityStaff?.Facility != null && facilityStaff.Facility.Status > 0)
                {
                    return _mapper.Map<VaccinationFacilityDTO>(facilityStaff.Facility);
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy cơ sở của manager: {ex.Message}");
            }
        }

        public async Task<VaccinationFacilityDTO> CreateFacilityAsync(CreateVaccinationFacilityDTO createDto, int managerAccountId)
        {
            try
            {
                // Kiểm tra manager đã có facility hoạt động chưa
                if (await CheckManagerHasFacilityAsync(managerAccountId))
                {
                    throw new InvalidOperationException("Manager này đã có cơ sở tiêm chủng hoạt động. Mỗi manager chỉ được tạo 1 cơ sở.");
                }

                // Kiểm tra account có tồn tại và có role Manager không
                var accountRepository = _unitOfWork.GetRepository<Account>();
                var account = await accountRepository.GetAsync(a => a.AccountId == managerAccountId && a.Status && a.Role == "Manager");
                if (account == null)
                {
                    throw new UnauthorizedAccessException("Account không tồn tại hoặc không có quyền Manager.");
                }

                // Kiểm tra số giấy phép đã tồn tại chưa
                var facilityRepository = _unitOfWork.GetRepository<VaccinationFacility>();
                var existingFacility = await facilityRepository.GetAsync(f => f.LicenseNumber == createDto.LicenseNumber && f.Status > 0);
                if (existingFacility != null)
                {
                    throw new InvalidOperationException("Số giấy phép này đã được sử dụng bởi cơ sở khác.");
                }

                // Tạo facility mới
                var facility = _mapper.Map<VaccinationFacility>(createDto);
                await facilityRepository.AddAsync(facility);
                await _unitOfWork.SaveChangesAsync();

                // Tạo FacilityStaff record cho Manager
                var facilityStaffRepository = _unitOfWork.GetRepository<FacilityStaff>();
                
                // Kiểm tra xem Manager đã có FacilityStaff record chưa
                var existingManagerStaff = await facilityStaffRepository.GetAsync(
                    fs => fs.AccountId == managerAccountId && fs.Position == "Manager"
                );

                if (existingManagerStaff != null)
                {
                    // Update FacilityId cho Manager đã tồn tại
                    existingManagerStaff.FacilityId = facility.FacilityId;
                    existingManagerStaff.Status = true;
                    existingManagerStaff.UpdatedAt = DateTime.UtcNow;
                    facilityStaffRepository.Update(existingManagerStaff);
                }
                else
                {
                    // Tạo FacilityStaff record mới cho Manager
                    var facilityStaff = new FacilityStaff
                    {
                        AccountId = managerAccountId,
                        FacilityId = facility.FacilityId,
                        FullName = account.AccountName,
                        Email = account.Email,
                        Position = "Manager",
                        Description = "Quản lý cơ sở",
                        Status = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await facilityStaffRepository.AddAsync(facilityStaff);
                }
                
                await _unitOfWork.SaveChangesAsync();

                return _mapper.Map<VaccinationFacilityDTO>(facility);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi tạo cơ sở: {ex.Message}");
            }
        }

        public async Task<VaccinationFacilityDTO?> UpdateFacilityAsync(UpdateVaccinationFacilityDTO updateDto, int managerAccountId)
        {
            try
            {
                var facilityRepository = _unitOfWork.GetRepository<VaccinationFacility>();
                var facility = await facilityRepository.GetAsync(f => f.FacilityId == updateDto.FacilityId && f.Status > 0);

                if (facility == null)
                {
                    throw new KeyNotFoundException("Không tìm thấy cơ sở.");
                }

                // Kiểm tra manager có quyền chỉnh sửa facility này không
                var facilityStaffRepository = _unitOfWork.GetRepository<FacilityStaff>();
                var facilityStaff = await facilityStaffRepository.GetAsync(
                    fs => fs.AccountId == managerAccountId && fs.FacilityId == updateDto.FacilityId && fs.Status && fs.FacilityId > 0
                );

                if (facilityStaff == null)
                {
                    throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa cơ sở này.");
                }

                // Kiểm tra số giấy phép có bị trùng với facility khác không
                if (facility.LicenseNumber != updateDto.LicenseNumber)
                {
                    var existingFacility = await facilityRepository.GetAsync(
                        f => f.LicenseNumber == updateDto.LicenseNumber && f.FacilityId != updateDto.FacilityId && f.Status > 0
                    );
                    if (existingFacility != null)
                    {
                        throw new InvalidOperationException("Số giấy phép này đã được sử dụng bởi cơ sở khác.");
                    }
                }

                // Cập nhật facility
                _mapper.Map(updateDto, facility);
                facilityRepository.Update(facility);
                await _unitOfWork.SaveChangesAsync();

                return _mapper.Map<VaccinationFacilityDTO>(facility);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi cập nhật cơ sở: {ex.Message}");
            }
        }

        public async Task<bool> DeleteFacilityAsync(int facilityId, int managerAccountId)
        {
            try
            {
                var facilityRepository = _unitOfWork.GetRepository<VaccinationFacility>();
                var facility = await facilityRepository.GetAsync(f => f.FacilityId == facilityId && f.Status > 0);

                if (facility == null)
                {
                    throw new KeyNotFoundException("Không tìm thấy cơ sở.");
                }

                // Kiểm tra manager có quyền xóa facility này không
                var facilityStaffRepository = _unitOfWork.GetRepository<FacilityStaff>();
                var facilityStaff = await facilityStaffRepository.GetAsync(
                    fs => fs.AccountId == managerAccountId && fs.FacilityId == facilityId && fs.Status && fs.FacilityId > 0
                );

                if (facilityStaff == null)
                {
                    throw new UnauthorizedAccessException("Bạn không có quyền xóa cơ sở này.");
                }

                // Soft delete - đặt status = 0
                facility.Status = 0;
                facility.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                facilityRepository.Update(facility);

                // Reset FacilityId của Manager về 0 để có thể tạo facility mới
                facilityStaff.FacilityId = 0; // Manager có thể tạo facility mới
                facilityStaff.Status = false; // Tạm thời vô hiệu hóa đến khi tạo facility mới
                facilityStaff.UpdatedAt = DateTime.UtcNow;
                var facilityStaffRepo = _unitOfWork.GetRepository<FacilityStaff>();
                facilityStaffRepo.Update(facilityStaff);

                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi xóa cơ sở: {ex.Message}");
            }
        }

        public async Task<bool> CheckManagerHasFacilityAsync(int managerAccountId)
        {
            try
            {
                var facilityStaffRepository = _unitOfWork.GetRepository<FacilityStaff>();
                return await facilityStaffRepository.AnyAsync(
                    fs => fs.AccountId == managerAccountId && fs.Position == "Manager" && fs.Status && fs.FacilityId > 0
                );
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi kiểm tra facility của manager: {ex.Message}");
            }
        }
    }
} 