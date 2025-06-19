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
                // Lấy Member từ AccountId
                var memberRepo = _unitOfWork.GetRepository<Member>();
                var member = await memberRepo.GetAsync(m => m.AccountId == accountId);

                if (member == null)
                {
                    throw new InvalidOperationException($"Không tìm thấy member cho account {accountId}");
                }

                // Lấy tất cả children của member này
                var childRepository = _unitOfWork.GetRepository<Child>();
                var children = await childRepository.FindAsync(c => c.MemberId == member.MemberId && c.Status == true);
                
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
                // Lấy Member từ AccountId
                var memberRepo = _unitOfWork.GetRepository<Member>();
                var member = await memberRepo.GetAsync(m => m.AccountId == accountId);

                if (member == null)
                {
                    throw new InvalidOperationException($"Không tìm thấy member cho account {accountId}");
                }

                // Lấy child và kiểm tra quyền sở hữu
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
                // Lấy Member từ AccountId
                var memberRepo = _unitOfWork.GetRepository<Member>();
                var member = await memberRepo.GetAsync(m => m.AccountId == accountId);

                if (member == null)
                {
                    throw new InvalidOperationException($"Không tìm thấy member cho account {accountId}. Cần đăng ký membership trước.");
                }

                // Kiểm tra số lượng trẻ tối đa theo gói membership
                var userMembershipRepo = _unitOfWork.GetRepository<UserMembership>();
                var activeMembership = await userMembershipRepo.GetAsync(
                    um => um.AccountId == accountId &&
                          um.Status == true &&
                          um.EndDate > DateTime.UtcNow,
                    includeProperties: "Membership"
                );

                if (activeMembership == null)
                {
                    throw new InvalidOperationException("Bạn cần có gói membership active để thêm trẻ");
                }

                // Kiểm tra số lượng trẻ hiện tại
                var childRepository = _unitOfWork.GetRepository<Child>();
                var currentChildrenCount = await childRepository.CountAsync(c => c.MemberId == member.MemberId && c.Status == true);

                // Giả sử Membership có field MaxChildren, nếu không thì set default
                int maxChildren = 5; // Default value, có thể lấy từ activeMembership.Membership.MaxChildren nếu có
                
                if (currentChildrenCount >= maxChildren)
                {
                    throw new InvalidOperationException($"Bạn đã đạt giới hạn số lượng trẻ ({maxChildren}) theo gói membership");
                }

                var child = _mapper.Map<Child>(childDTO);
                child.MemberId = member.MemberId; // Sử dụng MemberId thay vì UserId
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
                // Lấy Member từ AccountId
                var memberRepo = _unitOfWork.GetRepository<Member>();
                var member = await memberRepo.GetAsync(m => m.AccountId == accountId);

                if (member == null)
                {
                    throw new InvalidOperationException($"Không tìm thấy member cho account {accountId}");
                }

                // Lấy child và kiểm tra quyền sở hữu
                var childRepository = _unitOfWork.GetRepository<Child>();
                var child = await childRepository.GetAsync(c => c.ChildId == childId && c.MemberId == member.MemberId);

                if (child == null)
                {
                    throw new KeyNotFoundException($"Child with ID {childId} not found for account {accountId}");
                }

                _mapper.Map(childDTO, child);
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
                // Lấy Member từ AccountId
                var memberRepo = _unitOfWork.GetRepository<Member>();
                var member = await memberRepo.GetAsync(m => m.AccountId == accountId);

                if (member == null)
                {
                    throw new InvalidOperationException($"Không tìm thấy member cho account {accountId}");
                }

                // Lấy child và kiểm tra quyền sở hữu
                var childRepository = _unitOfWork.GetRepository<Child>();
                var child = await childRepository.GetAsync(c => c.ChildId == childId && c.MemberId == member.MemberId);

                if (child == null)
                {
                    throw new KeyNotFoundException($"Child with ID {childId} not found for account {accountId}");
                }

                // Soft delete
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
                // Lấy Member từ AccountId
                var memberRepo = _unitOfWork.GetRepository<Member>();
                var member = await memberRepo.GetAsync(m => m.AccountId == accountId);

                if (member == null)
                {
                    throw new InvalidOperationException($"Không tìm thấy member cho account {accountId}");
                }

                // Lấy child và kiểm tra quyền sở hữu
                var childRepository = _unitOfWork.GetRepository<Child>();
                var child = await childRepository.GetAsync(c => c.ChildId == childId && c.MemberId == member.MemberId);

                if (child == null)
                {
                    throw new KeyNotFoundException($"Child with ID {childId} not found for account {accountId}");
                }

                // Hard delete - có thể cần xóa các records liên quan trước
                // TODO: Xử lý cascade delete cho GrowthRecord, VaccinationAppointment, etc.
                
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
    }
}
