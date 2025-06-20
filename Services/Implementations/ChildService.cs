using AutoMapper;
using Contracts.DTOs.Child;
using Services.Interfaces;
using Repositories.Entities;
using Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class ChildService : IChildService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<ChildService> _logger;

        public ChildService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<ChildService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

        public async Task<ChildDTO> CreateChildAsync(int accountId, CreateChildDTO childDTO)
        {
            try
            {
                // Validation đầu vào
                ValidateCreateChildData(childDTO);

                // Tìm member
                var memberRepo = _unitOfWork.GetRepository<Member>();
                var member = await memberRepo.GetAsync(m => m.AccountId == accountId);

                if (member == null)
                {
                    throw new InvalidOperationException($"Không tìm thấy member cho account {accountId}. Cần đăng ký membership trước.");
                }

                // TODO: Kiểm tra membership validation khi production
                // Tạm thời bỏ membership validation cho development phase
                var childRepository = _unitOfWork.GetRepository<Child>();
                
                // Basic limit check - tối đa 10 trẻ per member
                var currentChildrenCount = await childRepository.CountAsync(c => c.MemberId == member.MemberId && c.Status == true);
                int maxChildren = 10; 
                
                if (currentChildrenCount >= maxChildren)
                {
                    throw new InvalidOperationException($"Bạn đã đạt giới hạn số lượng trẻ ({maxChildren})");
                }

                var child = _mapper.Map<Child>(childDTO);
                
                // Chuẩn hóa dữ liệu
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
                // Validation đầu vào
                ValidateUpdateChildData(childDTO);

                // Tìm member
                var memberRepo = _unitOfWork.GetRepository<Member>();
                var member = await memberRepo.GetAsync(m => m.AccountId == accountId);

                if (member == null)
                {
                    throw new InvalidOperationException($"Không tìm thấy member cho account {accountId}");
                }

                // Tìm child
                var childRepository = _unitOfWork.GetRepository<Child>();
                var child = await childRepository.GetAsync(c => c.ChildId == childId && c.MemberId == member.MemberId);

                if (child == null)
                {
                    throw new KeyNotFoundException($"Child with ID {childId} not found for account {accountId}");
                }

                _mapper.Map(childDTO, child);
                
                // Chuẩn hóa dữ liệu
                child.Gender = NormalizeGender(childDTO.Gender);
                child.BloodType = NormalizeBloodType(childDTO.BloodType);
                child.UpdateAt = DateTime.UtcNow;

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

        private void ValidateCreateChildData(CreateChildDTO childDTO)
        {
            var errors = new List<string>();

            // Kiểm tra tên
            if (string.IsNullOrWhiteSpace(childDTO.FullName))
            {
                errors.Add("Tên đầy đủ là bắt buộc");
            }
            else if (childDTO.FullName.Trim().Length < 2 || childDTO.FullName.Trim().Length > 100)
            {
                errors.Add("Tên phải từ 2-100 ký tự");
            }

            // Kiểm tra ngày sinh
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

            // Kiểm tra giới tính
            if (string.IsNullOrWhiteSpace(childDTO.Gender))
            {
                errors.Add("Giới tính là bắt buộc");
            }
            else if (!IsValidGender(childDTO.Gender))
            {
                errors.Add("Giới tính chỉ được nhận giá trị: Male hoặc Female");
            }

            // Kiểm tra nhóm máu (không bắt buộc)
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

            // Kiểm tra tên
            if (string.IsNullOrWhiteSpace(childDTO.FullName))
            {
                errors.Add("Tên đầy đủ là bắt buộc");
            }
            else if (childDTO.FullName.Trim().Length < 2 || childDTO.FullName.Trim().Length > 100)
            {
                errors.Add("Tên phải từ 2-100 ký tự");
            }

            // Kiểm tra ngày sinh
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

            // Kiểm tra giới tính
            if (string.IsNullOrWhiteSpace(childDTO.Gender))
            {
                errors.Add("Giới tính là bắt buộc");
            }
            else if (!IsValidGender(childDTO.Gender))
            {
                errors.Add("Giới tính chỉ được nhận giá trị: Male hoặc Female");
            }

            // Kiểm tra nhóm máu (không bắt buộc)
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
            if (string.IsNullOrWhiteSpace(bloodType)) return true; // Không bắt buộc
            
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
    }
}
