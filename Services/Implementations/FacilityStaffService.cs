using AutoMapper;
using Contracts.DTOs.Dashboard;
using Contracts.DTOs.FacilityStaff;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Repositories.Entities;
using Repositories.Interfaces;
using Repositories.Models.QueryModels;
using Services.Interfaces;
using System;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class FacilityStaffService : IFacilityStaffService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<FacilityStaffService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public FacilityStaffService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<FacilityStaffService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

        }
        private int GetCurrentAccountId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var accountIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(accountIdClaim, out int accountId))
                {
                    return accountId;
                }
            }
            return 0;
        }
        private async Task ValidateManagerAccess(int currentUserId)
        {
            var accountRepository = _unitOfWork.GetRepository<Account>();
            var currentAccount = await accountRepository.GetAsync(a => a.AccountId == currentUserId);
            if (currentAccount == null)
            {
                throw new UnauthorizedAccessException("Tài khoản không tồn tại");
            }

            if (currentAccount.Role == "Admin")
            {
                return;
            }

            if (currentAccount.Role == "FacilityStaff")
            {
                var staffRepository = _unitOfWork.GetRepository<FacilityStaff>();
                var staff = await staffRepository.GetAsync(s => s.AccountId == currentUserId);
                if (staff != null && staff.Position == "Manager")
                {
                    return;
                }
            }

            throw new UnauthorizedAccessException("Chỉ Admin hoặc Manager mới có quyền thực hiện hành động này");
        }
        public async Task<FacilityStaffDTO> UpdateFacilityStaffAsync(int staffId, UpdateFacilityStaffDTO staffDto)
        {
            _logger.LogInformation($"Updating facility staff with ID: {staffId}");

            if (staffDto == null)
            {
                throw new ArgumentNullException(nameof(staffDto), "Dữ liệu nhân viên cơ sở là bắt buộc");
            }

            var repository = _unitOfWork.GetRepository<FacilityStaff>();
            var staff = await repository.GetAsync(s => s.StaffId == staffId, includeProperties: "Account,Facility");
            if (staff == null)
            {
                throw new KeyNotFoundException($"Nhân viên cơ sở với ID {staffId} không tồn tại");
            }

            var currentUserId = GetCurrentAccountId();
            if (currentUserId == 0)
            {
                throw new UnauthorizedAccessException("Không thể xác định AccountId của người dùng hiện tại từ token");
            }

            // Kiểm tra quyền: Manager, Admin, hoặc chính nhân viên đó
            if (staff.AccountId != currentUserId)
            {
                await ValidateManagerAccess(currentUserId);
            }

            try
            {
                // Lưu các giá trị gốc cần bảo vệ
                var originalCreatedAt = staff.CreatedAt;
                var originalAccountId = staff.AccountId;
                var originalFacilityId = staff.FacilityId;

                // Ánh xạ DTO sang entity
                _mapper.Map(staffDto, staff);

                // Khôi phục các giá trị gốc
                staff.CreatedAt = originalCreatedAt;
                staff.AccountId = originalAccountId;
                staff.FacilityId = originalFacilityId;
                staff.UpdatedAt = DateTime.UtcNow; // Cập nhật UpdatedAt

                repository.Update(staff);
                await _unitOfWork.SaveChangesAsync();

                var updatedStaff = await repository.GetAsync(s => s.StaffId == staffId, includeProperties: "Account,Facility");
                return _mapper.Map<FacilityStaffDTO>(updatedStaff);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi cập nhật nhân viên cơ sở với ID {staffId}");
                throw;
            }
        }

        public async Task<FacilityStaffDTO> GetFacilityStaffByIdAsync(int staffId)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<FacilityStaff>();
                var staff = await repository.GetAsync(s => s.StaffId == staffId, includeProperties: "Account,Facility");
                if (staff == null)
                {
                    throw new KeyNotFoundException($"FacilityStaff với ID {staffId} không tồn tại");
                }
                return _mapper.Map<FacilityStaffDTO>(staff);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy nhân viên cơ sở {staffId}");
                throw;
            }
        }

        public async Task<QueryResultModel<IEnumerable<FacilityStaffDTO>>> GetAllFacilityStaffAsync(int? facilityId = null, string position = null, int? pageIndex = null, int? pageSize = null)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<FacilityStaff>();
                Expression<Func<FacilityStaff, bool>>? filter = null;
                if (facilityId.HasValue || !string.IsNullOrEmpty(position))
                {
                    filter = s => (!facilityId.HasValue || s.FacilityId == facilityId.Value) &&
                                  (string.IsNullOrEmpty(position) || s.Position == position);
                }

                var result = await repository.GetAllAsync(
                    filter: filter,
                    include: "Account,Facility",
                    pageIndex: pageIndex,
                    pageSize: pageSize
                );

                var dtos = _mapper.Map<IEnumerable<FacilityStaffDTO>>(result.Data);
                return new QueryResultModel<IEnumerable<FacilityStaffDTO>>
                {
                    TotalCount = result.TotalCount,
                    Data = dtos
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách nhân viên cơ sở");
                throw;
            }
        }
        public async Task<StaffCountsDTO> GetStaffCountsByFacilityAsync(int facilityId)
        {
            try
            {
                _logger.LogInformation($"Counting staff by position for FacilityId: {facilityId}");
                var repository = _unitOfWork.GetRepository<FacilityStaff>();
                var staffs = await repository.GetAllAsync(s => s.FacilityId == facilityId && s.Status == true);
                var counts = new StaffCountsDTO
                {
                    TotalStaffs = staffs.Data.Count(s => s.Position == "Staff"),
                    TotalManagers = staffs.Data.Count(s => s.Position == "Manager"),
                    TotalDoctors = staffs.Data.Count(s => s.Position == "Doctor")
                };

                return counts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error counting staff by position for FacilityId {facilityId}");
                throw;
            }
        }
    }
}