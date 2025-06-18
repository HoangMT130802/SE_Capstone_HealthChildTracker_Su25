using AutoMapper;
using BusinessLogic.DTOs.UserMembership;
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
    public class UserMembershipService : IUserMembershipService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<UserMembershipService> _logger;

        public UserMembershipService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<UserMembershipService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<UserMembershipDTO> GetUserMembershipByIdAsync(int userMembershipId)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<UserMembership>();
                var membership = await repository.GetAsync(
                    x => x.UserMembershipId == userMembershipId,
                    includeProperties: "User,Membership"
                );

                if (membership == null)
                {
                    _logger.LogWarning("Không tìm thấy membership với ID {Id}", userMembershipId);
                    return null;
                }

                var dto = _mapper.Map<UserMembershipDTO>(membership);
                if (dto == null)
                {
                    _logger.LogError("Lỗi khi map membership {Id} sang DTO", userMembershipId);
                    throw new InvalidOperationException("Lỗi khi chuyển đổi dữ liệu");
                }

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thông tin membership {Id}", userMembershipId);
                throw;
            }
        }

        public async Task<UserMembershipDTO> GetActiveUserMembershipAsync(int userId)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<UserMembership>();
                var membership = await repository.GetAsync(
                    x => x.UserId == userId &&
                         x.Status == "Active" &&
                         x.EndDate > DateTime.UtcNow,
                    includeProperties: "User,Membership"
                );

                return _mapper.Map<UserMembershipDTO>(membership);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy active membership của user {UserId}", userId);
                throw;
            }
        }

        public async Task<UserMembershipDTO> CreateUserMembershipAsync(CreateUserMembershipDTO dto)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Kiểm tra membership tồn tại
                var membershipRepo = _unitOfWork.GetRepository<Membership>();
                var membership = await membershipRepo.GetAsync(m => m.MembershipId == dto.MembershipId);
                if (membership == null)
                {
                    throw new KeyNotFoundException($"Không tìm thấy gói membership với ID {dto.MembershipId}");
                }

                // Kiểm tra số lượng request trong ngày
                var consultationRequestRepo = _unitOfWork.GetRepository<ConsultationRequest>();
                var today = DateTime.UtcNow.Date;
                var tomorrow = today.AddDays(1);

                var requestCount = await consultationRequestRepo.CountAsync(r =>
                    r.UserId == dto.UserId &&
                    r.CreatedAt >= today &&
                    r.CreatedAt < tomorrow);

                if (requestCount >= 2)
                {
                    throw new InvalidOperationException("Bạn đã đạt giới hạn số lần tạo yêu cầu tư vấn trong ngày (tối đa 2 lần/ngày)");
                }

                // Tạo user membership mới
                var repository = _unitOfWork.GetRepository<UserMembership>();
                var userMembership = _mapper.Map<UserMembership>(dto);

                // Set số lượt tư vấn từ gói membership
                userMembership.RemainingConsultations = membership.MaxConsultations; // Đổi từ ConsultationCount thành MaxConsultations

                await repository.AddAsync(userMembership);
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                return await GetUserMembershipByIdAsync(userMembership.UserMembershipId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi tạo membership mới cho user {UserId}", dto.UserId);
                throw;
            }
        }

        public async Task<bool> RenewMembershipAsync(int userMembershipId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var repository = _unitOfWork.GetRepository<UserMembership>();
                var membership = await repository.GetAsync(
                    x => x.UserMembershipId == userMembershipId,
                    includeProperties: "Membership"
                );

                if (membership == null)
                {
                    throw new KeyNotFoundException($"Không tìm thấy membership với ID {userMembershipId}");
                }

                // Kiểm tra số lượng request trong ngày
                var consultationRequestRepo = _unitOfWork.GetRepository<ConsultationRequest>();
                var today = DateTime.UtcNow.Date;
                var tomorrow = today.AddDays(1);

                var requestCount = await consultationRequestRepo.CountAsync(r =>
                    r.UserId == membership.UserId &&
                    r.CreatedAt >= today &&
                    r.CreatedAt < tomorrow);

                if (requestCount >= 2)
                {
                    throw new InvalidOperationException("Đã đạt giới hạn số lần tạo yêu cầu tư vấn trong ngày (tối đa 2 lần/ngày)");
                }

                membership.StartDate = DateTime.UtcNow;
                membership.EndDate = DateTime.UtcNow.AddMonths(12);
                membership.Status = "Active";
                membership.LastRenewalDate = DateTime.UtcNow;
                membership.RemainingConsultations = membership.Membership.MaxConsultations; 

                repository.Update(membership);
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi gia hạn membership {Id}", userMembershipId);
                throw;
            }
        }

        public async Task<bool> UpdateMembershipStatusAsync(int userMembershipId, string status)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<UserMembership>();
                var membership = await repository.GetAsync(x => x.UserMembershipId == userMembershipId);

                if (membership == null)
                {
                    throw new KeyNotFoundException($"Không tìm thấy membership với ID {userMembershipId}");
                }

                membership.Status = status;
                repository.Update(membership);
                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật trạng thái membership {Id}", userMembershipId);
                throw;
            }
        }

        public async Task<bool> DecrementConsultationCountAsync(int userMembershipId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var repository = _unitOfWork.GetRepository<UserMembership>();
                var membership = await repository.GetAsync(x => x.UserMembershipId == userMembershipId);

                if (membership == null)
                {
                    throw new KeyNotFoundException($"Không tìm thấy membership với ID {userMembershipId}");
                }

                if (membership.RemainingConsultations <= 0)
                {
                    throw new InvalidOperationException("Đã hết lượt tư vấn");
                }

                membership.RemainingConsultations--;
                repository.Update(membership);
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi giảm số lượt tư vấn của membership {Id}", userMembershipId);
                throw;
            }
        }
        public async Task<IEnumerable<UserMembershipDTO>> GetAllUserMembershipsAsync()
        {
            try
            {
                var repository = _unitOfWork.GetRepository<UserMembership>();
                var memberships = await repository.GetAllAsync(
                    includeProperties: "User,Membership"
                );

                return _mapper.Map<IEnumerable<UserMembershipDTO>>(memberships);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách tất cả user memberships");
                throw;
            }
        }
    }
}
