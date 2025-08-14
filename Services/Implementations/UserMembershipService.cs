using AutoMapper;
using Contracts.DTOs.UserMembership;
using Microsoft.Extensions.Logging;
using Repositories.Entities;
using Repositories.Interfaces;
using Repositories.Models.QueryModels;
using Services.Interfaces;

namespace Services.Implementations
{
    public class UserMembershipService : IUserMembershipService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<UserMembershipService> _logger;

        public UserMembershipService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<UserMembershipService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<UserMembershipResponseDTO> SubscribeMembershipAsync(int accountId, SubscribeMembershipDTO subscribeDto)
        {
            try
            {
                using var transaction = await _unitOfWork.BeginTransactionAsync();

                try
                {
                    // 1. Validate Account - chỉ cho phép Member
                    var accountRepository = _unitOfWork.GetRepository<Account>();
                    var account = await accountRepository.GetAsync(a => a.AccountId == accountId);
                    
                    if (account == null)
                    {
                        return new UserMembershipResponseDTO
                        {
                            IsSuccess = false,
                            Message = "Tài khoản không tồn tại"
                        };
                    }

                    if (account.Role != "Member")
                    {
                        return new UserMembershipResponseDTO
                        {
                            IsSuccess = false,
                            Message = "Chỉ Member mới có thể sử dụng API này. Guest vui lòng sử dụng API /guest-subscribe"
                        };
                    }

                    // 2. Validate Membership
                    var membershipRepository = _unitOfWork.GetRepository<Membership>();
                    var membership = await membershipRepository.GetAsync(m => m.MembershipId == subscribeDto.MembershipId && m.Status == true);
                    
                    if (membership == null)
                    {
                        return new UserMembershipResponseDTO
                        {
                            IsSuccess = false,
                            Message = "Gói membership không tồn tại hoặc đã bị vô hiệu hóa"
                        };
                    }

                    // 3. Check if user already has active membership
                    var userMembershipRepository = _unitOfWork.GetRepository<UserMembership>();
                    var existingActiveMembership = await userMembershipRepository.GetAsync(um => 
                        um.AccountId == accountId && 
                        um.Status == true && 
                        um.EndDate > DateTime.UtcNow);

                    if (existingActiveMembership != null)
                    {
                        return new UserMembershipResponseDTO
                        {
                            IsSuccess = false,
                            Message = "Bạn đã có membership đang hoạt động. Vui lòng chờ hết hạn hoặc hủy membership hiện tại trước khi đăng ký mới."
                        };
                    }

                    // 4. Create UserMembership - StartDate luôn là hôm nay
                    var startDate = DateTime.UtcNow;
                    var endDate = startDate.AddMonths(membership.Duration);

                    var userMembership = new UserMembership
                    {
                        AccountId = accountId,
                        MembershipId = subscribeDto.MembershipId,
                        StartDate = startDate,
                        EndDate = endDate,
                        Status = true,      
                        LastRenewalDate = DateOnly.FromDateTime(startDate)
                    };

                    await userMembershipRepository.AddAsync(userMembership);
                    await _unitOfWork.SaveChangesAsync();

                    await transaction.CommitAsync();

                    // 5. Prepare response
                    var userMembershipWithNavigation = await userMembershipRepository.GetAsync(
                        um => um.UserMembershipId == userMembership.UserMembershipId,
                        includeProperties: "Account,Membership"
                    );

                    var userMembershipDto = _mapper.Map<UserMembershipDTO>(userMembershipWithNavigation);

                    _logger.LogInformation($"Member {account.AccountName} successfully subscribed to membership {membership.Name}");

                    return new UserMembershipResponseDTO
                    {
                        IsSuccess = true,
                        Message = $"Đăng ký gói {membership.Name} thành công!",
                        UserMembership = userMembershipDto
                    };
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error subscribing to membership for account {accountId}");
                return new UserMembershipResponseDTO
                {
                    IsSuccess = false,
                    Message = "Có lỗi xảy ra khi đăng ký membership. Vui lòng thử lại sau."
                };
            }
        }

        public async Task<UserMembershipResponseDTO> GuestSubscribeMembershipAsync(int accountId, GuestSubscribeMembershipDTO subscribeDto)
        {
            try
            {
                using var transaction = await _unitOfWork.BeginTransactionAsync();

                try
                {
                    // 1. Validate Account - chỉ cho phép Guest
                    var accountRepository = _unitOfWork.GetRepository<Account>();
                    var account = await accountRepository.GetAsync(a => a.AccountId == accountId);
                    
                    if (account == null)
                    {
                        return new UserMembershipResponseDTO
                        {
                            IsSuccess = false,
                            Message = "Tài khoản không tồn tại"
                        };
                    }

                    if (account.Role != "Guest")
                    {
                        return new UserMembershipResponseDTO
                        {
                            IsSuccess = false,
                            Message = "Chỉ Guest mới có thể sử dụng API này. Member vui lòng sử dụng API /subscribe"
                        };
                    }

                    // 2. Validate Membership
                    var membershipRepository = _unitOfWork.GetRepository<Membership>();
                    var membership = await membershipRepository.GetAsync(m => m.MembershipId == subscribeDto.MembershipId && m.Status == true);
                    
                    if (membership == null)
                    {
                        return new UserMembershipResponseDTO
                        {
                            IsSuccess = false,
                            Message = "Gói membership không tồn tại hoặc đã bị vô hiệu hóa"
                        };
                    }

                    // 3. Upgrade Guest to Member
                    var memberRepository = _unitOfWork.GetRepository<Member>();
                    var newMember = new Member
                    {
                        AccountId = accountId,
                        FullName = subscribeDto.FullName,
                        PhoneNumber = subscribeDto.PhoneNumber,
                        Address = subscribeDto.Address ?? "",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await memberRepository.AddAsync(newMember);
                    await _unitOfWork.SaveChangesAsync(); // Save để get MemberId

                    // Update Account role
                    account.Role = "Member";
                    account.UpdatedAt = DateTime.UtcNow;
                    accountRepository.Update(account);

                    // 4. Create UserMembership - StartDate luôn là hôm nay
                    var startDate = DateTime.UtcNow;
                    var endDate = startDate.AddMonths(membership.Duration);

                    var userMembershipRepository = _unitOfWork.GetRepository<UserMembership>();
                    var userMembership = new UserMembership
                    {
                        AccountId = accountId,
                        MembershipId = subscribeDto.MembershipId,
                        StartDate = startDate,
                        EndDate = endDate,
                        Status = true,
   
                        LastRenewalDate = DateOnly.FromDateTime(startDate)
                    };

                    await userMembershipRepository.AddAsync(userMembership);
                    await _unitOfWork.SaveChangesAsync();

                    await transaction.CommitAsync();

                    // 5. Prepare response
                    var userMembershipWithNavigation = await userMembershipRepository.GetAsync(
                        um => um.UserMembershipId == userMembership.UserMembershipId,
                        includeProperties: "Account,Membership"
                    );

                    var userMembershipDto = _mapper.Map<UserMembershipDTO>(userMembershipWithNavigation);

                    _logger.LogInformation($"Guest {account.AccountName} upgraded to Member (MemberId: {newMember.MemberId}) and subscribed to membership {membership.Name}");

                    return new UserMembershipResponseDTO
                    {
                        IsSuccess = true,
                        Message = $"Chúc mừng! Bạn đã được nâng cấp thành Member và đăng ký gói {membership.Name} thành công!",
                        UserMembership = userMembershipDto
                    };
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error guest subscribing to membership for account {accountId}");
                return new UserMembershipResponseDTO
                {
                    IsSuccess = false,
                    Message = "Có lỗi xảy ra khi đăng ký membership. Vui lòng thử lại sau."
                };
            }
        }

        public async Task<List<UserMembershipDTO>> GetUserMembershipsAsync(int accountId)
        {
            try
            {
                var userMembershipRepository = _unitOfWork.GetRepository<UserMembership>();
                var userMemberships = await userMembershipRepository.FindAsync(
                    um => um.AccountId == accountId,
                    includeProperties: "Account,Membership"
                );

                // Sắp xếp theo StartDate descending
                var sortedMemberships = userMemberships.OrderByDescending(um => um.StartDate).ToList();

                return _mapper.Map<List<UserMembershipDTO>>(sortedMemberships);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting user memberships for account {accountId}");
                throw;
            }
        }

        public async Task<UserMembershipDTO> GetActiveUserMembershipAsync(int accountId)
        {
            try
            {
                var userMembershipRepository = _unitOfWork.GetRepository<UserMembership>();
                var activeMembership = await userMembershipRepository.GetAsync(
                    um => um.AccountId == accountId && 
                          um.Status == true && 
                          um.StartDate <= DateTime.UtcNow && 
                          um.EndDate > DateTime.UtcNow,
                    includeProperties: "Account,Membership"
                );

                return activeMembership != null ? _mapper.Map<UserMembershipDTO>(activeMembership) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting active user membership for account {accountId}");
                throw;
            }
        }

        public async Task<bool> CancelUserMembershipAsync(int userMembershipId, int accountId)
        {
            try
            {
                var userMembershipRepository = _unitOfWork.GetRepository<UserMembership>();
                var userMembership = await userMembershipRepository.GetAsync(
                    um => um.UserMembershipId == userMembershipId && um.AccountId == accountId
                );

                if (userMembership == null)
                {
                    throw new KeyNotFoundException("UserMembership không tồn tại hoặc không thuộc về tài khoản này");
                }

                if (!userMembership.Status)
                {
                    throw new InvalidOperationException("Membership đã bị hủy trước đó");
                }

                userMembership.Status = false;
                userMembership.EndDate = DateTime.UtcNow; // Kết thúc ngay lập tức

                userMembershipRepository.Update(userMembership);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"UserMembership {userMembershipId} cancelled by account {accountId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cancelling user membership {userMembershipId} for account {accountId}");
                throw;
            }
        }

        public async Task<bool> RenewUserMembershipAsync(int userMembershipId, int accountId)
        {
            try
            {
                var userMembershipRepository = _unitOfWork.GetRepository<UserMembership>();
                var userMembership = await userMembershipRepository.GetAsync(
                    um => um.UserMembershipId == userMembershipId && um.AccountId == accountId,
                    includeProperties: "Membership"
                );

                if (userMembership == null)
                {
                    throw new KeyNotFoundException("UserMembership không tồn tại hoặc không thuộc về tài khoản này");
                }

                if (!userMembership.Status)
                {
                    throw new InvalidOperationException("Không thể gia hạn membership đã bị hủy");
                }

                // Extend end date
                var newEndDate = userMembership.EndDate.AddMonths(userMembership.Membership.Duration);
                userMembership.EndDate = newEndDate;
                userMembership.LastRenewalDate = DateOnly.FromDateTime(DateTime.UtcNow);

                userMembershipRepository.Update(userMembership);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"UserMembership {userMembershipId} renewed by account {accountId} until {newEndDate}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error renewing user membership {userMembershipId} for account {accountId}");
                throw;
            }
        }

        public async Task<QueryResultModel<List<UserMembershipDTO>>> GetAllUserMembershipsAsync(int pageIndex = 1, int pageSize = 10, bool? status = null, int? membershipId = null)
        {
            try
            {
                var userMembershipRepository = _unitOfWork.GetRepository<UserMembership>();
                
                var result = await userMembershipRepository.GetAllAsync(
                    filter: um => (status == null || um.Status == status) && 
                                  (membershipId == null || um.MembershipId == membershipId),
                    orderBy: q => q.OrderByDescending(um => um.StartDate),
                    include: "Account,Membership",
                    pageIndex: pageIndex,
                    pageSize: pageSize
                );

                var userMembershipDTOs = _mapper.Map<List<UserMembershipDTO>>(result.Data);

                return new QueryResultModel<List<UserMembershipDTO>>
                {
                    Data = userMembershipDTOs,
                    TotalCount = result.TotalCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all user memberships");
                throw;
            }
        }
        public async Task<int> GetActiveCountAsync()
        {
            var repository = _unitOfWork.GetRepository<UserMembership>();
            return await repository.CountAsync(um => um.Status == true);
        }
    }
} 