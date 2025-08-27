using AutoMapper;
using Contracts.DTOs.Child;
using Contracts.DTOs.GrowthRecord;
using Services.Interfaces;
using Repositories.Entities;
using Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Repositories.Models;
using Microsoft.Extensions.Options;

namespace Services.Implementations
{
    public class ChildService : IChildService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<ChildService> _logger;
        private readonly Cloudinary _cloudinary;
        public ChildService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<ChildService> logger, IOptions<CloudinarySettings> cloudinaryConfig)
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
        private async Task<string> UploadImageToCloudinary(IFormFile image)
        {
            if (image == null || image.Length == 0)
                throw new ArgumentException("Image is required");

            using var stream = image.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(image.FileName, stream),
                Folder = "child_images"
            };
            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            return uploadResult.SecureUrl.AbsoluteUri;
        }
        public async Task<IEnumerable<ChildDTO>> GetAllChildrenByAccountIdAsync(int accountId)
        {
            try
            {
                _logger.LogInformation($"ChildService: Getting children for AccountId: {accountId}");
                
              
                var memberRepo = _unitOfWork.GetRepository<Member>();
                var member = await memberRepo.GetAsync(m => m.AccountId == accountId);

                _logger.LogInformation($"ChildService: Member lookup result: {(member != null ? $"Found MemberId {member.MemberId}" : "Not found")}");

                if (member == null)
                {
                    _logger.LogWarning($"ChildService: No member found for AccountId {accountId}");
                    throw new InvalidOperationException($"Không tìm thấy member cho account {accountId}");
                }

               
                var childRepository = _unitOfWork.GetRepository<Child>();
                var children = await childRepository.FindAsync(c => c.MemberId == member.MemberId && c.Status == true);
                
                _logger.LogInformation($"ChildService: Found {children.Count()} children for MemberId {member.MemberId}");
                
                return _mapper.Map<IEnumerable<ChildDTO>>(children);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting children for account {accountId}");
                throw;
            }
        }

        public async Task<ChildDTO> GetChildByIdAsync(int childId, int accountId)
        {
            try
            {
              
                var memberRepo = _unitOfWork.GetRepository<Member>();
                var member = await memberRepo.GetAsync(m => m.AccountId == accountId);

                if (member == null)
                {
                    throw new InvalidOperationException($"Không tìm thấy member cho account {accountId}");
                }

                
                var childRepository = _unitOfWork.GetRepository<Child>();
                var child = await childRepository.GetAsync(c => c.ChildId == childId && c.MemberId == member.MemberId);

                if (child == null)
                {
                    throw new KeyNotFoundException($"Child with ID {childId} not found for account {accountId}");
                }

                return _mapper.Map<ChildDTO>(child);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting child {childId} for account {accountId}");
                throw;
            }
        }

        /// <summary>
        /// Lấy thông tin child theo childId mà không cần check account ownership (public API)
        /// </summary>
        public async Task<ChildDTO> GetChildByIdPublicAsync(int childId)
        {
            try
            {
                var childRepository = _unitOfWork.GetRepository<Child>();
                var child = await childRepository.GetAsync(c => c.ChildId == childId && c.Status == true);

                if (child == null)
                {
                    throw new KeyNotFoundException($"Child with ID {childId} not found");
                }

                return _mapper.Map<ChildDTO>(child);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting public child {childId}");
                throw;
            }
        }

        public async Task<ChildDTO> CreateChildAsync(int accountId, CreateChildDTO childDTO)
        {
            try
            {        
                ValidateCreateChildData(childDTO);
               
                var memberRepo = _unitOfWork.GetRepository<Member>();
                var member = await memberRepo.GetAsync(m => m.AccountId == accountId);

                if (member == null)
                {
                    throw new InvalidOperationException($"Không tìm thấy member cho account {accountId}. Cần đăng ký membership trước.");
                }

                // TODO: Kiểm tra membership validation khi production
                // Tạm thời bỏ membership validation cho development phase
                var childRepository = _unitOfWork.GetRepository<Child>();
                
              
                var currentChildrenCount = await childRepository.CountAsync(c => c.MemberId == member.MemberId && c.Status == true);
                int maxChildren = 10; 
                
                if (currentChildrenCount >= maxChildren)
                {
                    throw new InvalidOperationException($"Bạn đã đạt giới hạn số lượng trẻ ({maxChildren})");
                }

                var child = _mapper.Map<Child>(childDTO);
                
             
                child.Gender = NormalizeGender(childDTO.Gender);
                child.BloodType = NormalizeBloodType(childDTO.BloodType);
                child.MemberId = member.MemberId; 
                child.Status = true;
                child.CreatedAt = DateTime.UtcNow;
                child.UpdateAt = DateTime.UtcNow;

                await childRepository.AddAsync(child);
                await _unitOfWork.SaveChangesAsync();

                return _mapper.Map<ChildDTO>(child);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating child for account {accountId}");
                throw;
            }
        }

        public async Task<ChildDTO> UpdateChildAsync(int childId, int accountId, UpdateChildDTO childDTO)
        {
            try
            {
                ValidateUpdateChildData(childDTO);

                var memberRepo = _unitOfWork.GetRepository<Member>();
                var member = await memberRepo.GetAsync(m => m.AccountId == accountId);

                if (member == null)
                {
                    throw new InvalidOperationException($"Không tìm thấy member cho account {accountId}");
                }

                var childRepository = _unitOfWork.GetRepository<Child>();
                var child = await childRepository.GetAsync(c => c.ChildId == childId && c.MemberId == member.MemberId);

                if (child == null)
                {
                    throw new KeyNotFoundException($"Child with ID {childId} not found for account {accountId}");
                }

                var originalCreatedAt = child.CreatedAt;

                _mapper.Map(childDTO, child);

                child.CreatedAt = originalCreatedAt;
                child.Gender = NormalizeGender(childDTO.Gender);
                child.BloodType = NormalizeBloodType(childDTO.BloodType);
                child.UpdateAt = DateTime.UtcNow;

                if (childDTO.Image != null)
                {
                    child.ImageUrl = await UploadImageToCloudinary(childDTO.Image);
                }

                childRepository.Update(child);
                await _unitOfWork.SaveChangesAsync();

                return _mapper.Map<ChildDTO>(child);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating child {childId} for account {accountId}");
                throw;
            }
        }

        public async Task<bool> SoftDeleteChildAsync(int childId, int accountId)
        {
            try
            {
                
                var memberRepo = _unitOfWork.GetRepository<Member>();
                var member = await memberRepo.GetAsync(m => m.AccountId == accountId);

                if (member == null)
                {
                    throw new InvalidOperationException($"Không tìm thấy member cho account {accountId}");
                }

                var childRepository = _unitOfWork.GetRepository<Child>();
                var child = await childRepository.GetAsync(c => c.ChildId == childId && c.MemberId == member.MemberId);

                if (child == null)
                {
                    throw new KeyNotFoundException($"Child with ID {childId} not found for account {accountId}");
                }

                child.Status = false;
                child.UpdateAt = DateTime.UtcNow;

                childRepository.Update(child);
                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting child {childId} for account {accountId}");
                throw;
            }
        }

        public async Task<bool> HardDeleteChildAsync(int childId, int accountId)
        {
            try
            {
                var memberRepo = _unitOfWork.GetRepository<Member>();
                var member = await memberRepo.GetAsync(m => m.AccountId == accountId);

                if (member == null)
                {
                    throw new InvalidOperationException($"Không tìm thấy member cho account {accountId}");
                }

              
                var childRepository = _unitOfWork.GetRepository<Child>();
                var child = await childRepository.GetAsync(c => c.ChildId == childId && c.MemberId == member.MemberId);

                if (child == null)
                {
                    throw new KeyNotFoundException($"Child with ID {childId} not found for account {accountId}");
                }

                // Hard delete - có thể cần xóa các records liên quan trước
                // TODO: Xử lý cascade delete cho GrowthRecord, VaccinationAppointment, ..
                
                childRepository.Delete(child);
                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error hard deleting child {childId} for account {accountId}");
                throw;
            }
        }

        #region Private Validation Methods

        public async Task<ChildWithGrowthRecordResponseDTO> CreateChildWithGrowthRecordAsync(int accountId, CreateChildWithGrowthRecordDTO createDTO)
        {
            try
            {
               
                ValidateCreateChildWithGrowthRecordData(createDTO);

                var memberRepo = _unitOfWork.GetRepository<Member>();
                var member = await memberRepo.GetAsync(m => m.AccountId == accountId);

                if (member == null)
                {
                    throw new InvalidOperationException($"Không tìm thấy member cho account {accountId}. Cần đăng ký membership trước.");
                }

                var childRepository = _unitOfWork.GetRepository<Child>();
                
                // Basic limit check - tối đa 10 trẻ per member
                var currentChildrenCount = await childRepository.CountAsync(c => c.MemberId == member.MemberId && c.Status == true);
                int maxChildren = 10; 
                
                if (currentChildrenCount >= maxChildren)
                {
                    throw new InvalidOperationException($"Bạn đã đạt giới hạn số lượng trẻ ({maxChildren})");
                }

                // Tạo child trước
                var child = new Child
                {
                    FullName = createDTO.FullName?.Trim(),
                    BirthDate = createDTO.BirthDate,
                    Gender = NormalizeGender(createDTO.Gender),
                    BloodType = NormalizeBloodType(createDTO.BloodType),
                    AllergiesNotes = createDTO.AllergiesNotes?.Trim(),
                    MedicalHistory = createDTO.MedicalHistory?.Trim(),
                    MemberId = member.MemberId,
                    Status = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdateAt = DateTime.UtcNow
                };

                await childRepository.AddAsync(child);
                await _unitOfWork.SaveChangesAsync();

               
                var growthRecordRepository = _unitOfWork.GetRepository<GrowthRecord>();
                
                // Tính BMI
                decimal heightInMeters = createDTO.Height / 100;
                decimal bmi = Math.Round(createDTO.Weight / (heightInMeters * heightInMeters), 2);

                var growthRecord = new GrowthRecord
                {
                    ChildId = child.ChildId,
                    Height = createDTO.Height,
                    Weight = createDTO.Weight,
                    HeadCircumference = createDTO.HeadCircumference ?? 0,
                    Bmi = bmi,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Note = createDTO.GrowthNote?.Trim()
                };

                await growthRecordRepository.AddAsync(growthRecord);
                await _unitOfWork.SaveChangesAsync();

               
                var savedChild = await childRepository.GetAsync(c => c.ChildId == child.ChildId);
                var savedGrowthRecord = await growthRecordRepository.GetAsync(
                    r => r.RecordId == growthRecord.RecordId,
                    includeProperties: "Child"
                );

              
                var childDTO = _mapper.Map<ChildDTO>(savedChild);
                var growthRecordDTO = _mapper.Map<GrowthRecordDTO>(savedGrowthRecord);

                return new ChildWithGrowthRecordResponseDTO
                {
                    Child = childDTO,
                    GrowthRecord = growthRecordDTO,
                    Message = "Trẻ em và bản ghi tăng trưởng đã được tạo thành công"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating child with growth record for account {accountId}");
                throw;
            }
        }

        private void ValidateCreateChildWithGrowthRecordData(CreateChildWithGrowthRecordDTO createDTO)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(createDTO.FullName))
            {
                errors.Add("Tên đầy đủ là bắt buộc");
            }
            else if (createDTO.FullName.Trim().Length < 2 || createDTO.FullName.Trim().Length > 100)
            {
                errors.Add("Tên phải từ 2-100 ký tự");
            }

            if (createDTO.BirthDate == default)
            {
                errors.Add("Ngày sinh là bắt buộc");
            }
            else if (createDTO.BirthDate > DateTime.Now)
            {
                errors.Add("Ngày sinh không thể là tương lai");
            }
            else if (createDTO.BirthDate < DateTime.Now.AddYears(-18))
            {
                errors.Add("Trẻ em phải dưới 18 tuổi");
            }

            if (string.IsNullOrWhiteSpace(createDTO.Gender))
            {
                errors.Add("Giới tính là bắt buộc");
            }
            else if (!IsValidGender(createDTO.Gender))
            {
                errors.Add("Giới tính chỉ được nhận giá trị: Male hoặc Female");
            }

            if (!string.IsNullOrWhiteSpace(createDTO.BloodType) && !IsValidBloodType(createDTO.BloodType))
            {
                errors.Add("Nhóm máu phải theo định dạng: A, B, AB, O (có thể có + hoặc -)");
            }

            if (createDTO.Height <= 0 || createDTO.Height < 30 || createDTO.Height > 250)
            {
                errors.Add("Chiều cao phải từ 30-250 cm");
            }

            if (createDTO.Weight <= 0 || createDTO.Weight < 0.5m || createDTO.Weight > 200)
            {
                errors.Add("Cân nặng phải từ 0.5-200 kg");
            }

            if (createDTO.HeadCircumference.HasValue && 
                (createDTO.HeadCircumference.Value < 20 || createDTO.HeadCircumference.Value > 80))
            {
                errors.Add("Vòng đầu phải từ 20-80 cm");
            }

            if (errors.Any())
            {
                throw new ArgumentException($"Dữ liệu không hợp lệ: {string.Join("; ", errors)}");
            }
        }

        private void ValidateCreateChildData(CreateChildDTO childDTO)
        {
            var errors = new List<string>();
         
            if (string.IsNullOrWhiteSpace(childDTO.FullName))
            {
                errors.Add("Tên đầy đủ là bắt buộc");
            }
            else if (childDTO.FullName.Trim().Length < 2 || childDTO.FullName.Trim().Length > 100)
            {
                errors.Add("Tên phải từ 2-100 ký tự");
            }
            
            if (childDTO.BirthDate == default)
            {
                errors.Add("Ngày sinh là bắt buộc");
            }
            else if (childDTO.BirthDate > DateTime.Now)
            {
                errors.Add("Ngày sinh không thể là tương lai");
            }
            else if (childDTO.BirthDate < DateTime.Now.AddYears(-18))
            {
                errors.Add("Trẻ em phải dưới 18 tuổi");
            }

            if (string.IsNullOrWhiteSpace(childDTO.Gender))
            {
                errors.Add("Giới tính là bắt buộc");
            }
            else if (!IsValidGender(childDTO.Gender))
            {
                errors.Add("Giới tính chỉ được nhận giá trị: Male hoặc Female");
            }

            
            if (!string.IsNullOrWhiteSpace(childDTO.BloodType) && !IsValidBloodType(childDTO.BloodType))
            {
                errors.Add("Nhóm máu phải theo định dạng: A, B, AB, O (có thể có + hoặc -)");
            }

            if (errors.Any())
            {
                throw new ArgumentException($"Dữ liệu không hợp lệ: {string.Join("; ", errors)}");
            }
        }

        private void ValidateUpdateChildData(UpdateChildDTO childDTO)
        {
            var errors = new List<string>();

            
            if (string.IsNullOrWhiteSpace(childDTO.FullName))
            {
                errors.Add("Tên đầy đủ là bắt buộc");
            }
            else if (childDTO.FullName.Trim().Length < 2 || childDTO.FullName.Trim().Length > 100)
            {
                errors.Add("Tên phải từ 2-100 ký tự");
            }

           
            if (childDTO.BirthDate == default)
            {
                errors.Add("Ngày sinh là bắt buộc");
            }
            else if (childDTO.BirthDate > DateTime.Now)
            {
                errors.Add("Ngày sinh không thể là tương lai");
            }
            else if (childDTO.BirthDate < DateTime.Now.AddYears(-18))
            {
                errors.Add("Trẻ em phải dưới 18 tuổi");
            }

            if (string.IsNullOrWhiteSpace(childDTO.Gender))
            {
                errors.Add("Giới tính là bắt buộc");
            }
            else if (!IsValidGender(childDTO.Gender))
            {
                errors.Add("Giới tính chỉ được nhận giá trị: Male hoặc Female");
            }

           
            if (!string.IsNullOrWhiteSpace(childDTO.BloodType) && !IsValidBloodType(childDTO.BloodType))
            {
                errors.Add("Nhóm máu phải theo định dạng: A, B, AB, O (có thể có + hoặc -)");
            }

            if (errors.Any())
            {
                throw new ArgumentException($"Dữ liệu không hợp lệ: {string.Join("; ", errors)}");
            }
        }

        private bool IsValidGender(string gender)
        {
            if (string.IsNullOrWhiteSpace(gender)) return false;
            
            var normalizedGender = gender.Trim().ToLowerInvariant();
            return normalizedGender == "male" || normalizedGender == "female";
        }

        private bool IsValidBloodType(string bloodType)
        {
            if (string.IsNullOrWhiteSpace(bloodType)) return true; 
            
            var normalizedBloodType = bloodType.Trim().ToUpperInvariant();
            var validBloodTypes = new[] { "A", "B", "AB", "O", "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };
            
            return validBloodTypes.Contains(normalizedBloodType);
        }

        private string NormalizeGender(string gender)
        {
            if (string.IsNullOrWhiteSpace(gender)) return null;
            
            var normalizedGender = gender.Trim().ToLowerInvariant();
            return normalizedGender == "male" ? "Male" : 
                   normalizedGender == "female" ? "Female" : gender;
        }

        private string NormalizeBloodType(string bloodType)
        {
            if (string.IsNullOrWhiteSpace(bloodType)) return null;
            
            return bloodType.Trim().ToUpperInvariant();
        }

        #endregion
        public async Task<int> GetTotalCountAsync()
        {
            var repository = _unitOfWork.GetRepository<Child>();
            return await repository.CountAsync(a => true); 
        }
    }
}
