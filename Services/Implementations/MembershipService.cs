using AutoMapper;
using Contracts.DTOs.Membership;
using Microsoft.Extensions.Logging;
using Repositories.Entities;
using Repositories.Interfaces;
using Repositories.Models.QueryModels;
using Services.Interfaces;

namespace Services.Implementations
{
    public class MembershipService : IMembershipService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<MembershipService> _logger;

        public MembershipService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<MembershipService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<QueryResultModel<List<MembershipDTO>>> GetAllMembershipsAsync(int pageIndex = 1, int pageSize = 10, bool? status = null)
        {
            try
            {
                var membershipRepository = _unitOfWork.GetRepository<Membership>();
                
                var result = await membershipRepository.GetAllAsync(
                    filter: status.HasValue ? m => m.Status == status.Value : null,
                    orderBy: q => q.OrderByDescending(m => m.CreatedAt),
                    pageIndex: pageIndex,
                    pageSize: pageSize
                );

                var membershipDTOs = _mapper.Map<List<MembershipDTO>>(result.Data);

                _logger.LogInformation($"Retrieved {membershipDTOs.Count} memberships (page {pageIndex}, size {pageSize})");
                
                return new QueryResultModel<List<MembershipDTO>>
                {
                    Data = membershipDTOs,
                    TotalCount = result.TotalCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all memberships");
                throw;
            }
        }

        public async Task<MembershipDTO> GetMembershipByIdAsync(int membershipId)
        {
            try
            {
                var membershipRepository = _unitOfWork.GetRepository<Membership>();
                var membership = await membershipRepository.GetAsync(m => m.MembershipId == membershipId);

                if (membership == null)
                {
                    throw new KeyNotFoundException($"Membership with ID {membershipId} not found");
                }

                return _mapper.Map<MembershipDTO>(membership);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting membership {membershipId}");
                throw;
            }
        }

        public async Task<MembershipDTO> CreateMembershipAsync(CreateMembershipDTO createDto)
        {
            try
            {
                // Validate unique name
                var membershipRepository = _unitOfWork.GetRepository<Membership>();
                var existingMembership = await membershipRepository.GetAsync(m => 
                    m.Name.ToLower() == createDto.Name.ToLower());

                if (existingMembership != null)
                {
                    throw new InvalidOperationException($"Gói membership với tên '{createDto.Name}' đã tồn tại");
                }

                var membership = _mapper.Map<Membership>(createDto);
                membership.CreatedAt = DateTime.UtcNow;
                membership.UpdatedAt = DateTime.UtcNow;

                await membershipRepository.AddAsync(membership);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Created membership: {membership.Name} (ID: {membership.MembershipId})");
                return _mapper.Map<MembershipDTO>(membership);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating membership: {createDto.Name}");
                throw;
            }
        }

        public async Task<MembershipDTO> UpdateMembershipAsync(int membershipId, UpdateMembershipDTO updateDto)
        {
            try
            {
                var membershipRepository = _unitOfWork.GetRepository<Membership>();
                var membership = await membershipRepository.GetAsync(m => m.MembershipId == membershipId);

                if (membership == null)
                {
                    throw new KeyNotFoundException($"Membership with ID {membershipId} not found");
                }

                // Validate unique name if name is being updated
                if (!string.IsNullOrEmpty(updateDto.Name) && updateDto.Name.ToLower() != membership.Name.ToLower())
                {
                    var existingMembership = await membershipRepository.GetAsync(m => 
                        m.Name.ToLower() == updateDto.Name.ToLower());

                    if (existingMembership != null)
                    {
                        throw new InvalidOperationException($"Gói membership với tên '{updateDto.Name}' đã tồn tại");
                    }
                }

                _mapper.Map(updateDto, membership);
                membership.UpdatedAt = DateTime.UtcNow;

                membershipRepository.Update(membership);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Updated membership: {membership.Name} (ID: {membershipId})");
                return _mapper.Map<MembershipDTO>(membership);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating membership {membershipId}");
                throw;
            }
        }

        public async Task<bool> DeleteMembershipAsync(int membershipId)
        {
            try
            {
                var membershipRepository = _unitOfWork.GetRepository<Membership>();
                var membership = await membershipRepository.GetAsync(m => m.MembershipId == membershipId);

                if (membership == null)
                {
                    throw new KeyNotFoundException($"Membership with ID {membershipId} not found");
                }

                // Check if membership is being used by any users
                if (membership.UserMemberships != null && membership.UserMemberships.Any())
                {
                    throw new InvalidOperationException("Không thể xóa gói membership đang được sử dụng bởi người dùng");
                }

                membershipRepository.Delete(membership);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Deleted membership: {membership.Name} (ID: {membershipId})");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting membership {membershipId}");
                throw;
            }
        }

        public async Task<bool> ToggleMembershipStatusAsync(int membershipId)
        {
            try
            {
                var membershipRepository = _unitOfWork.GetRepository<Membership>();
                var membership = await membershipRepository.GetAsync(m => m.MembershipId == membershipId);

                if (membership == null)
                {
                    throw new KeyNotFoundException($"Membership with ID {membershipId} not found");
                }

                membership.Status = !membership.Status;
                membership.UpdatedAt = DateTime.UtcNow;

                membershipRepository.Update(membership);
                await _unitOfWork.SaveChangesAsync();

                string statusText = membership.Status ? "activated" : "deactivated";
                _logger.LogInformation($"Membership {membership.Name} (ID: {membershipId}) {statusText}");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error toggling membership status {membershipId}");
                throw;
            }
        }

        public async Task<List<MembershipDTO>> GetActiveMembershipsAsync()
        {
            try
            {
                var membershipRepository = _unitOfWork.GetRepository<Membership>();
                var result = await membershipRepository.GetAllAsync(
                    filter: m => m.Status == true,
                    orderBy: q => q.OrderBy(m => m.Price)
                );

                return _mapper.Map<List<MembershipDTO>>(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active memberships");
                throw;
            }
        }
    }
} 