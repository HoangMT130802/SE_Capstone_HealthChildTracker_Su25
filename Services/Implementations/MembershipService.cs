using AutoMapper;
using BusinessLogic.DTOs.Membership;
using BusinessLogic.Services.Interfaces;
using DataAccess.Entities;
using DataAccess.UnitOfWork;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Implementations
{
    public class MembershipService : IMembershipService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<MembershipService> _logger;

        public MembershipService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<MembershipService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<MembershipDTO>> GetAllMembershipsAsync()
        {
            try
            {
                var membershipRepo = _unitOfWork.GetRepository<Membership>();
                var memberships = await membershipRepo.GetAllAsync();
                return _mapper.Map<IEnumerable<MembershipDTO>>(memberships);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách gói membership");
                throw;
            }
        }

        public async Task<MembershipDTO> GetMembershipByIdAsync(int membershipId)
        {
            try
            {
                var membershipRepo = _unitOfWork.GetRepository<Membership>();
                var membership = await membershipRepo.GetAsync(m => m.MembershipId == membershipId);

                if (membership == null)
                {
                    throw new KeyNotFoundException($"Không tìm thấy gói membership với ID {membershipId}");
                }

                return _mapper.Map<MembershipDTO>(membership);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy thông tin gói membership {membershipId}");
                throw;
            }
        }

        public async Task<MembershipDTO> UpdateMembershipPriceAsync(int membershipId, decimal newPrice)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var membershipRepo = _unitOfWork.GetRepository<Membership>();
                var membership = await membershipRepo.GetAsync(m => m.MembershipId == membershipId);

                if (membership == null)
                {
                    throw new KeyNotFoundException($"Không tìm thấy gói membership với ID {membershipId}");
                }

                if (newPrice <= 0)
                {
                    throw new InvalidOperationException("Giá mới phải lớn hơn 0");
                }

                membership.Price = newPrice;
                membershipRepo.Update(membership);
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Đã cập nhật giá gói membership {membershipId} thành {newPrice}");
                return _mapper.Map<MembershipDTO>(membership);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Lỗi khi cập nhật giá gói membership {membershipId}");
                throw;
            }
        }

        public async Task<MembershipDTO> UpdateMembershipStatusAsync(int membershipId, bool status)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var membershipRepo = _unitOfWork.GetRepository<Membership>();
                var membership = await membershipRepo.GetAsync(m => m.MembershipId == membershipId);

                if (membership == null)
                {
                    throw new KeyNotFoundException($"Không tìm thấy gói membership với ID {membershipId}");
                }

                membership.Status = status;
                membershipRepo.Update(membership);
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Đã cập nhật trạng thái gói membership {membershipId} thành {status}");
                return _mapper.Map<MembershipDTO>(membership);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Lỗi khi cập nhật trạng thái gói membership {membershipId}");
                throw;
            }
        }
     /*   public async Task<MembershipDTO> UpdateMembershipAsync(int membershipId, UpdateMembershipDTO updateDto)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var membershipRepo = _unitOfWork.GetRepository<Membership>();
                var membership = await membershipRepo.GetAsync(m => m.MembershipId == membershipId);

                if (membership == null)
                {
                    throw new KeyNotFoundException($"Không tìm thấy gói membership với ID {membershipId}");
                }

                // Cập nhật thông tin
                membership.Name = updateDto.Name;
                membership.Description = updateDto.Description;
                membership.Duration = updateDto.Duration;
                membership.Price = updateDto.Price;
                membership.MaxChildren = updateDto.MaxChildren;
                membership.MaxConsultations = updateDto.MaxConsultations;
                membership.MaxAppointment = updateDto.MaxAppointment;
                membership.CanAccessAppoinment = updateDto.CanAccessAppoinment;
                membership.CanAccessConsultation = updateDto.CanAccessConsultation;
                membership.Status = updateDto.Status;

                membershipRepo.Update(membership);
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Đã cập nhật thông tin gói membership {membershipId}");
                return _mapper.Map<MembershipDTO>(membership);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Lỗi khi cập nhật thông tin gói membership {membershipId}");
                throw;
            }
        }*/
    }
}
