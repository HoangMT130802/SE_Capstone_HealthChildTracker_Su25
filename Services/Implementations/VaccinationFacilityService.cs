using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Contracts.DTOs.VaccinationFacility;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Repositories.Common;
using Repositories.Entities;
using Repositories.Interfaces;
using Repositories.Models;
using Repositories.Models.QueryModels;
using Services.Interfaces;
using System.Linq;

namespace Services.Implementations
{
    public class VaccinationFacilityService : IVaccinationFacilityService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly Cloudinary _cloudinary;
        public VaccinationFacilityService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<VaccinationFacilityService> logger, IOptions<CloudinarySettings> cloudinaryConfig)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            var config = cloudinaryConfig.Value;
            if (string.IsNullOrEmpty(config.CloudName) || string.IsNullOrEmpty(config.ApiKey) || string.IsNullOrEmpty(config.ApiSecret))
            {
                throw new ArgumentException("Cloudinary configuration is incomplete or invalid.");
            }
            _cloudinary = new Cloudinary(new CloudinaryDotNet.Account(
                config.CloudName,
                config.ApiKey,
                config.ApiSecret
            ));
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
        private async Task<string> UploadFileToCloudinary(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is required");

            // Giới hạn dung lượng 5MB
            if (file.Length > 5 * 1024 * 1024)
                throw new ArgumentException("File size must not exceed 5MB");

            // Chỉ cho phép pdf, jpg, jpeg, png
            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
                throw new ArgumentException("File must be a PDF or image (jpg, jpeg, png)");

            using var stream = file.OpenReadStream();

            UploadResult uploadResult;

            if (fileExtension == ".pdf")
            {
                // PDF => dùng RawUploadParams
                var uploadParams = new RawUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = "facility_licenses",
                    UseFilename = true,
                    UniqueFilename = false
                };

                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }
            else
            {
                // Ảnh => dùng ImageUploadParams
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = "facility_licenses",
                    UseFilename = true,
                    UniqueFilename = false
                };

                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }

            if (uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
            {
                _logger.LogError($"Cloudinary upload failed: Status={uploadResult.StatusCode}, Error={uploadResult.Error?.Message}");
                throw new InvalidOperationException($"Failed to upload file to Cloudinary: {uploadResult.Error?.Message}");
            }

            _logger.LogInformation($"Uploaded file: {uploadResult.SecureUrl}");
            return uploadResult.SecureUrl.AbsoluteUri;
        }



        private string ExtractPublicIdFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            var uri = new Uri(url);
            var path = uri.AbsolutePath;
            var segments = path.Split('/');
            var index = Array.IndexOf(segments, "facility_licenses");
            if (index != -1 && index + 1 < segments.Length)
            {
                return $"facility_licenses/{segments[index + 1]}";
            }
            return null;
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
                _logger.LogError(ex, "Error retrieving all facilities");
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
                _logger.LogError(ex, $"Error retrieving facility {facilityId}");
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
                    includeProperties: "Facility"
                );

                if (facilityStaff?.Facility != null && facilityStaff.Facility.Status > 0)
                {
                    return _mapper.Map<VaccinationFacilityDTO>(facilityStaff.Facility);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving facility for manager {accountId}");
                throw new Exception($"Lỗi khi lấy cơ sở của manager: {ex.Message}");
            }
        }

        public async Task<VaccinationFacilityDTO> CreateFacilityAsync(CreateVaccinationFacilityDTO createDto, int managerAccountId)
        {
            try
            {
                if (await CheckManagerHasFacilityAsync(managerAccountId))
                {
                    throw new InvalidOperationException("Manager này đã có cơ sở tiêm chủng hoạt động. Mỗi manager chỉ được tạo 1 cơ sở.");
                }

                var accountRepository = _unitOfWork.GetRepository<Repositories.Entities.Account>();
                var account = await accountRepository.GetAsync(a => a.AccountId == managerAccountId && a.Status && a.Role == "FacilityStaff");
                if (account == null)
                {
                    throw new UnauthorizedAccessException("Account không tồn tại hoặc không có quyền FacilityStaff.");
                }

                var facilityRepository = _unitOfWork.GetRepository<VaccinationFacility>();
                var existingFacility = await facilityRepository.GetAsync(f => f.LicenseNumber == createDto.LicenseNumber && f.Status > 0);
                if (existingFacility != null)
                {
                    throw new InvalidOperationException("Số giấy phép này đã được sử dụng bởi cơ sở khác.");
                }

                using var transaction = await _unitOfWork.BeginTransactionAsync();

                try
                {
                    var facility = _mapper.Map<VaccinationFacility>(createDto);
                    facility.Status = 1;
                    facility.CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    facility.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    facility.LicenseFile = await UploadFileToCloudinary(createDto.LicenseFile);

                    await facilityRepository.AddAsync(facility);
                    await _unitOfWork.SaveChangesAsync();

                    var facilityStaffRepository = _unitOfWork.GetRepository<FacilityStaff>();
                    var existingManagerStaff = await facilityStaffRepository.GetAsync(
                        fs => fs.AccountId == managerAccountId && fs.Position == "Manager"
                    );

                    if (existingManagerStaff != null)
                    {
                        existingManagerStaff.FacilityId = facility.FacilityId;
                        existingManagerStaff.Status = true;
                        existingManagerStaff.UpdatedAt = DateTime.UtcNow;
                        facilityStaffRepository.Update(existingManagerStaff);
                    }
                    else
                    {
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
                    await transaction.CommitAsync();

                    return _mapper.Map<VaccinationFacilityDTO>(facility);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating facility for manager {managerAccountId}");
                throw new Exception($"Lỗi khi tạo cơ sở: {ex.Message}");
            }
        }

        public async Task<VaccinationFacilityDTO> UpdateFacilityInfoAsync(UpdateVaccinationFacilityDTO updateDto, int managerAccountId)
        {
            try
            {
                var facilityRepository = _unitOfWork.GetRepository<VaccinationFacility>();
                var facility = await facilityRepository.GetAsync(f => f.FacilityId == updateDto.FacilityId && f.Status > 0);

                if (facility == null)
                {
                    throw new KeyNotFoundException("Không tìm thấy cơ sở.");
                }

                // Kiểm tra vai trò của tài khoản
                var accountRepository = _unitOfWork.GetRepository<Repositories.Entities.Account>();
                var account = await accountRepository.GetAsync(a => a.AccountId == managerAccountId);

                if (account == null)
                {
                    throw new KeyNotFoundException("Tài khoản không tồn tại.");
                }

                // Nếu không phải Admin, kiểm tra FacilityStaff
                if (account.Role != "Admin")
                {
                    var facilityStaffRepository = _unitOfWork.GetRepository<FacilityStaff>();
                    var facilityStaff = await facilityStaffRepository.GetAsync(
                        fs => fs.AccountId == managerAccountId && fs.FacilityId == updateDto.FacilityId && fs.Status && fs.FacilityId > 0
                    );

                    if (facilityStaff == null)
                    {
                        throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa cơ sở này.");
                    }
                }

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

                using var transaction = await _unitOfWork.BeginTransactionAsync();

                try
                {
                    // Lưu CreatedAt gốc
                    var originalCreatedAt = facility.CreatedAt;

                    // Xóa file cũ trên Cloudinary nếu có file mới
                    if (updateDto.LicenseFile != null && !string.IsNullOrEmpty(facility.LicenseFile))
                    {
                        var publicId = ExtractPublicIdFromUrl(facility.LicenseFile);
                        if (!string.IsNullOrEmpty(publicId))
                        {
                            await _cloudinary.DestroyAsync(new DeletionParams(publicId));
                        }
                    }

                    // Cập nhật facility
                    _mapper.Map(updateDto, facility);
                    facility.CreatedAt = originalCreatedAt;
                    facility.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                    // Tải lên file mới nếu có
                    if (updateDto.LicenseFile != null)
                    {
                        facility.LicenseFile = await UploadFileToCloudinary(updateDto.LicenseFile);
                    }

                    facilityRepository.Update(facility);
                    await _unitOfWork.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var response = _mapper.Map<VaccinationFacilityDTO>(facility);

                    _logger.LogInformation($"Facility {facility.FacilityName} updated successfully");
                    return response;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating facility {updateDto.FacilityId}");
                throw new Exception($"Lỗi khi cập nhật thông tin cơ sở: {ex.Message}");
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
        public async Task<int> GetTotalCountAsync()
        {
            var repository = _unitOfWork.GetRepository<VaccinationFacility>();
            return await repository.CountAsync(a => a.Status == 1);
        }
    }
} 