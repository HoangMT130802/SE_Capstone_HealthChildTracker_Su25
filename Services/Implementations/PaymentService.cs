using AutoMapper;
using Contracts.DTOs.Transaction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Net.payOS;
using Net.payOS.Types;
using Repositories.Entities;
using Repositories.Interfaces;
using Services.Interfaces;
using Transaction = Repositories.Entities.Transaction;

namespace Services.Implementations
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<PaymentService> _logger;
        private readonly PayOS _payOS;
        private readonly IConfiguration _configuration;

        
        public PaymentService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<PaymentService> logger,
            PayOS payOS,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _payOS = payOS;
            _configuration = configuration;

        }

        public async Task<PaymentDetailResponseDTO> CreatePaymentAsync(PaymentRequestDTO request)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("Bắt đầu tạo payment cho Account {AccountId}", request.AccountId);

                // Xác định loại transaction và tính amount/description (KHÔNG tạo bản ghi membership/subscription ở đây)
                string transactionType;
                decimal amount;
                string description;
                int itemId;

                if (request.MembershipId.HasValue)
                {
                    transactionType = "User";
                    (amount, description) = await GetUserMembershipPaymentInfo(request);
                    itemId = request.MembershipId.Value;
                }
                else if (request.FacilityMembershipId.HasValue)
                {
                    transactionType = "Facility";
                    (amount, description) = await GetFacilityMembershipPaymentInfo(request);
                    itemId = request.FacilityMembershipId.Value;
                }
                else
                {
                    throw new ArgumentException("Phải có MembershipId hoặc FacilityMembershipId");
                }

                // Tạo mã giao dịch unique, encode cả itemId để xử lý khi PAID
                // Format: {ticks}_{accountId}_{type}_{itemId}
                string orderCode = $"{DateTimeOffset.Now.ToUnixTimeMilliseconds()}_{request.AccountId}_{transactionType}_{itemId}";

                // Đảm bảo description không quá 25 ký tự (giới hạn PayOS)
                string shortDescription = description.Length > 25 ? description.Substring(0, 25) : description;
                _logger.LogInformation("PayOS Data - OrderCode: {OrderCode}, Amount: {Amount}, Description: '{Description}' (Length: {Length}), TransactionType: '{TransactionType}'", 
                    orderCode, amount, shortDescription, shortDescription.Length, transactionType);
                
                // Tạo payment link với PayOS
                var paymentData = new PaymentData(
                    long.Parse(orderCode.Split('_')[0]),
                    (int)amount,
                    shortDescription,
                    new List<ItemData>(),
                    $"{GetBaseUrl()}/payment/cancel",
                    $"{GetBaseUrl()}/payment/success?orderId={orderCode}",
                    null
                );

                var createPayment = await _payOS.createPaymentLink(paymentData);

                // Tạo membership/subscription với status PENDING và gán ID vào transaction
                int? userMembershipId = null;
                int? facilityMembershipSubscriptionId = null;

                if (request.MembershipId.HasValue)
                {
                    // Tạo UserMembership với status Pending
                    userMembershipId = await CreatePendingUserMembership(request.AccountId, request.MembershipId.Value);
                }
                else if (request.FacilityMembershipId.HasValue)
                {
                    // Tạo FacilityMembershipSubscription với status Pending  
                    facilityMembershipSubscriptionId = await CreatePendingFacilitySubscription(request.AccountId, request.FacilityMembershipId.Value);
                }

                // Lưu transaction vào database với membership/subscription ID đã tạo
                var newTransaction = new Transaction
                {
                    UserMembershipId = userMembershipId,
                    FacilityMembershipSubscriptionId = facilityMembershipSubscriptionId,
                    TransactionType = transactionType,
                    Amount = amount,
                    PaymentMethod = "PAYOS",
                    TransactionCode = orderCode,
                    Description = description,
                    Status = "PENDING",
                    CreatedAt = DateOnly.FromDateTime(DateTime.UtcNow)
                };

                var transactionRepo = _unitOfWork.GetRepository<Transaction>();
                await transactionRepo.AddAsync(newTransaction);
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Tạo payment thành công. OrderCode: {OrderCode}, PaymentUrl: {PaymentUrl}", 
                    orderCode, createPayment.checkoutUrl);

                // Tự động check status và kích hoạt membership sau khi tạo payment
                var finalStatus = await AutoCheckAndActivatePayment(orderCode, userMembershipId, facilityMembershipSubscriptionId);

                return new PaymentDetailResponseDTO
                {
                    PaymentUrl = createPayment.checkoutUrl,
                    OrderId = orderCode,
                    Amount = amount,
                    Status = finalStatus,
                    ReturnUrl = $"{GetBaseUrl()}/payment/success?orderId={orderCode}",
                    CancelUrl = $"{GetBaseUrl()}/payment/cancel",
                    UserMembershipId = userMembershipId,
                    FacilityMembershipSubscriptionId = facilityMembershipSubscriptionId,
                    TransactionType = transactionType,
                    Description = description
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi tạo payment cho Account {AccountId}", request.AccountId);
                throw;
            }
        }

        // Tự động check PayOS và kích hoạt membership sau khi tạo payment
        private async Task<string> AutoCheckAndActivatePayment(string orderCode, int? userMembershipId, int? facilityMembershipSubscriptionId)
        {
            try
            {
                _logger.LogInformation("Bắt đầu auto-check payment status cho OrderCode: {OrderCode}", orderCode);
                
                // Đợi một chút để PayOS cập nhật trạng thái (nếu user thanh toán nhanh)
                await Task.Delay(2000);
                
                // Retry logic: thử check PayOS nhiều lần trong 30 giây
                for (int attempt = 1; attempt <= 15; attempt++)
                {
                    try
                    {
                        var paymentInfo = await _payOS.getPaymentLinkInformation(long.Parse(orderCode.Split('_')[0]));
                        _logger.LogInformation("PayOS Status (attempt {Attempt}): {Status} cho OrderCode: {OrderCode}", 
                            attempt, paymentInfo.status, orderCode);

                        if (paymentInfo.status.Equals("PAID", StringComparison.OrdinalIgnoreCase))
                        {
                            // Cập nhật transaction thành PAID
                            var transactionRepo = _unitOfWork.GetRepository<Transaction>();
                            var transaction = await transactionRepo.GetAsync(t => t.TransactionCode == orderCode);
                            
                            if (transaction != null && !string.Equals(transaction.Status, "PAID", StringComparison.OrdinalIgnoreCase))
                            {
                                transaction.Status = "PAID";
                                transaction.Amount = paymentInfo.amount;
                                transactionRepo.Update(transaction);

                                // Kích hoạt membership/subscription
                                if (userMembershipId.HasValue)
                                {
                                    await ActivateUserMembership(userMembershipId.Value);
                                    _logger.LogInformation("Đã kích hoạt UserMembership Id: {UserMembershipId} cho OrderCode: {OrderCode}", 
                                        userMembershipId.Value, orderCode);
                                }
                                else if (facilityMembershipSubscriptionId.HasValue)
                                {
                                    await ActivateFacilitySubscription(facilityMembershipSubscriptionId.Value);
                                    _logger.LogInformation("Đã kích hoạt FacilitySubscription Id: {SubscriptionId} cho OrderCode: {OrderCode}", 
                                        facilityMembershipSubscriptionId.Value, orderCode);
                                }

                                await _unitOfWork.SaveChangesAsync();
                                _logger.LogInformation("Auto-check thành công: Payment PAID và đã kích hoạt membership cho OrderCode: {OrderCode}", orderCode);
                                return "PAID";
                            }
                        }
                        else if (paymentInfo.status.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase))
                        {
                            // Cập nhật transaction thành CANCELLED
                            var transactionRepo = _unitOfWork.GetRepository<Transaction>();
                            var transaction = await transactionRepo.GetAsync(t => t.TransactionCode == orderCode);
                            
                            if (transaction != null)
                            {
                                transaction.Status = "CANCELLED";
                                transactionRepo.Update(transaction);
                                await _unitOfWork.SaveChangesAsync();
                                _logger.LogInformation("Auto-check: Payment CANCELLED cho OrderCode: {OrderCode}", orderCode);
                                return "CANCELLED";
                            }
                        }
                        
                        // Nếu vẫn PENDING, đợi 2 giây rồi thử lại
                        if (attempt < 15)
                        {
                            await Task.Delay(2000);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Lỗi khi check PayOS attempt {Attempt} cho OrderCode: {OrderCode}", attempt, orderCode);
                        if (attempt < 15)
                        {
                            await Task.Delay(2000);
                        }
                    }
                }

                _logger.LogInformation("Auto-check timeout: Payment vẫn PENDING sau 30 giây cho OrderCode: {OrderCode}", orderCode);
                return "PENDING";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi trong quá trình auto-check payment cho OrderCode: {OrderCode}", orderCode);
                return "PENDING";
            }
        }

        public async Task<bool> ProcessPaymentWebhookAsync(string orderId, string status, decimal amount)
        {
            // Vẫn giữ phương thức cho tương thích, nhưng luồng chính dùng CheckPaymentStatusAsync theo yêu cầu
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("Xử lý webhook payment. OrderId: {OrderId}, Status: {Status}, Amount: {Amount}", 
                    orderId, status, amount);

                var transactionRepo = _unitOfWork.GetRepository<Transaction>();
                var existingTransaction = await transactionRepo.GetAsync(t => t.TransactionCode == orderId);

                if (existingTransaction == null)
                {
                    _logger.LogWarning("Không tìm thấy transaction cho OrderId: {OrderId}", orderId);
                    return false;
                }

                if (string.Equals(existingTransaction.Status, status, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                existingTransaction.Status = status;
                existingTransaction.Amount = amount;
                transactionRepo.Update(existingTransaction);

                if (status.Equals("PAID", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(existingTransaction.TransactionType, "User", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!existingTransaction.UserMembershipId.HasValue)
                        {
                            var userMembershipId = await CreateUserMembershipIfNotExists(existingTransaction.TransactionCode);
                            existingTransaction.UserMembershipId = userMembershipId;
                        }
                    }
                    else if (string.Equals(existingTransaction.TransactionType, "Facility", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!existingTransaction.FacilityMembershipSubscriptionId.HasValue)
                        {
                            var subscriptionId = await CreateFacilitySubscriptionIfNotExists(existingTransaction.TransactionCode);
                            existingTransaction.FacilityMembershipSubscriptionId = subscriptionId;
                        }
                    }
                }

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi xử lý webhook cho OrderId: {OrderId}", orderId);
                throw;
            }
        }

        #region Private Helper Methods

        // Chỉ tính amount + validate, KHÔNG tạo bản ghi
        private async Task<(decimal Amount, string Description)> GetUserMembershipPaymentInfo(PaymentRequestDTO request)
        {
            if (!request.MembershipId.HasValue)
            {
                throw new ArgumentException("MembershipId là bắt buộc cho UserMembership");
            }

            var accountRepo = _unitOfWork.GetRepository<Account>();
            var membershipRepo = _unitOfWork.GetRepository<Membership>();
            var userMembershipRepo = _unitOfWork.GetRepository<UserMembership>();

            var account = await accountRepo.GetByIdAsync(request.AccountId) ?? throw new ArgumentException("Không tìm thấy tài khoản");
            var membership = await membershipRepo.GetByIdAsync(request.MembershipId.Value) ?? throw new ArgumentException("Không tìm thấy gói membership");

            // Không cho tạo nếu đã có active
            var activeMembership = await userMembershipRepo.GetAsync(um => um.AccountId == request.AccountId && um.Status == true);
            if (activeMembership != null)
            {
                throw new InvalidOperationException($"Tài khoản đã có gói membership đang hoạt động. Gói hiện tại sẽ tự động gia hạn khi hết hạn vào {activeMembership.EndDate:dd/MM/yyyy}");
            }

            return (membership.Price, "Goi thanh vien");
        }

        // Chỉ tính amount + validate, KHÔNG tạo bản ghi
        private async Task<(decimal Amount, string Description)> GetFacilityMembershipPaymentInfo(PaymentRequestDTO request)
        {
            if (!request.FacilityMembershipId.HasValue)
            {
                throw new ArgumentException("FacilityMembershipId là bắt buộc cho FacilityMembership");
            }

            var facilityStaffRepo = _unitOfWork.GetRepository<FacilityStaff>();
            var facilityStaff = await facilityStaffRepo.GetAsync(fs => fs.AccountId == request.AccountId) ?? throw new ArgumentException("Tài khoản không phải là FacilityStaff");

            var facilityId = facilityStaff.FacilityId;

            var accountRepo = _unitOfWork.GetRepository<Account>();
            var facilityRepo = _unitOfWork.GetRepository<VaccinationFacility>();
            var facilityMembershipRepo = _unitOfWork.GetRepository<FacilityMembership>();
            var subscriptionRepo = _unitOfWork.GetRepository<FacilityMembershipSubscription>();

            _ = await accountRepo.GetByIdAsync(request.AccountId) ?? throw new ArgumentException("Không tìm thấy tài khoản");
            _ = await facilityRepo.GetByIdAsync(facilityId) ?? throw new ArgumentException("Không tìm thấy cơ sở");
            var facilityMembership = await facilityMembershipRepo.GetByIdAsync(request.FacilityMembershipId.Value) ?? throw new ArgumentException("Không tìm thấy gói membership cho cơ sở");

            var activeSubscription = await subscriptionRepo.GetAsync(s => s.FacilityId == facilityId && s.Status == true);
            if (activeSubscription != null)
            {
                throw new InvalidOperationException($"Cơ sở đã có gói membership đang hoạt động. Gói hiện tại sẽ tự động gia hạn khi hết hạn vào {activeSubscription.EndDate:dd/MM/yyyy}");
            }

            return (facilityMembership.Price, "Goi co so");
        }

        // Tạo UserMembership với status Pending ngay khi tạo payment
        private async Task<int> CreatePendingUserMembership(int accountId, int membershipId)
        {
            var userMembershipRepo = _unitOfWork.GetRepository<UserMembership>();
            var membershipRepo = _unitOfWork.GetRepository<Membership>();

            var membership = await membershipRepo.GetByIdAsync(membershipId) ?? throw new ArgumentException("Không tìm thấy gói membership");

            var newUserMembership = new UserMembership
            {
                AccountId = accountId,
                MembershipId = membershipId,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(membership.Duration),
                Status = false, // Pending - chưa active
                LastRenewalDate = DateOnly.FromDateTime(DateTime.UtcNow)
            };

            await userMembershipRepo.AddAsync(newUserMembership);
            await _unitOfWork.SaveChangesAsync();
            return newUserMembership.UserMembershipId;
        }

        // Tạo FacilityMembershipSubscription với status Pending ngay khi tạo payment
        private async Task<int> CreatePendingFacilitySubscription(int accountId, int facilityMembershipId)
        {
            var facilityStaffRepo = _unitOfWork.GetRepository<FacilityStaff>();
            var subscriptionRepo = _unitOfWork.GetRepository<FacilityMembershipSubscription>();
            var facilityMembershipRepo = _unitOfWork.GetRepository<FacilityMembership>();

            var staff = await facilityStaffRepo.GetAsync(fs => fs.AccountId == accountId) ?? throw new ArgumentException("Tài khoản không phải là FacilityStaff");
            var facilityId = staff.FacilityId;
            var facilityMembership = await facilityMembershipRepo.GetByIdAsync(facilityMembershipId) ?? throw new ArgumentException("Không tìm thấy gói membership cho cơ sở");

            var newSubscription = new FacilityMembershipSubscription
            {
                FacilityId = facilityId,
                FacilityMembershipId = facilityMembershipId,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(facilityMembership.Duration),
                Status = false, // Pending - chưa active
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await subscriptionRepo.AddAsync(newSubscription);
            await _unitOfWork.SaveChangesAsync();
            return newSubscription.SubscriptionId;
        }

        // Kích hoạt UserMembership (chuyển từ Pending sang Active khi PAID)
        private async Task ActivateUserMembership(int userMembershipId)
        {
            var userMembershipRepo = _unitOfWork.GetRepository<UserMembership>();
            var userMembership = await userMembershipRepo.GetByIdAsync(userMembershipId);
            
            if (userMembership != null)
            {
                userMembership.Status = true; // Active
                userMembership.StartDate = DateTime.UtcNow;
                userMembership.LastRenewalDate = DateOnly.FromDateTime(DateTime.UtcNow);
                userMembershipRepo.Update(userMembership);
            }
        }

        // Kích hoạt FacilitySubscription (chuyển từ Pending sang Active khi PAID)
        private async Task ActivateFacilitySubscription(int subscriptionId)
        {
            var subscriptionRepo = _unitOfWork.GetRepository<FacilityMembershipSubscription>();
            var subscription = await subscriptionRepo.GetByIdAsync(subscriptionId);
            
            if (subscription != null)
            {
                subscription.Status = true; // Active
                subscription.StartDate = DateTime.UtcNow;
                subscription.UpdatedAt = DateTime.UtcNow;
                subscriptionRepo.Update(subscription);
            }
        }

        // Tạo UserMembership mới nếu chưa có (khi PAID) và trả về id
        private async Task<int> CreateUserMembershipIfNotExists(string orderCode)
        {
            var parts = orderCode.Split('_');
            if (parts.Length < 4 || !int.TryParse(parts[1], out var accountId) || !int.TryParse(parts[3], out var membershipId))
            {
                throw new InvalidOperationException("OrderCode không hợp lệ");
            }

            var userMembershipRepo = _unitOfWork.GetRepository<UserMembership>();
            var membershipRepo = _unitOfWork.GetRepository<Membership>();

            var membership = await membershipRepo.GetByIdAsync(membershipId) ?? throw new ArgumentException("Không tìm thấy gói membership");

            // Nếu đã có active thì không tạo nữa
            var activeMembership = await userMembershipRepo.GetAsync(um => um.AccountId == accountId && um.Status == true);
            if (activeMembership != null)
            {
                return activeMembership.UserMembershipId;
            }

            var newUserMembership = new UserMembership
            {
                AccountId = accountId,
                MembershipId = membershipId,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(membership.Duration),
                Status = true, // tạo trực tiếp active vì đã PAID
                LastRenewalDate = DateOnly.FromDateTime(DateTime.UtcNow)
            };

            await userMembershipRepo.AddAsync(newUserMembership);
            await _unitOfWork.SaveChangesAsync();
            return newUserMembership.UserMembershipId;
        }

        // Tạo FacilityMembershipSubscription mới nếu chưa có (khi PAID) và trả về id
        private async Task<int> CreateFacilitySubscriptionIfNotExists(string orderCode)
        {
            var parts = orderCode.Split('_');
            if (parts.Length < 4 || !int.TryParse(parts[1], out var accountId) || !int.TryParse(parts[3], out var facilityMembershipId))
            {
                throw new InvalidOperationException("OrderCode không hợp lệ");
            }

            var facilityStaffRepo = _unitOfWork.GetRepository<FacilityStaff>();
            var subscriptionRepo = _unitOfWork.GetRepository<FacilityMembershipSubscription>();
            var facilityMembershipRepo = _unitOfWork.GetRepository<FacilityMembership>();

            var staff = await facilityStaffRepo.GetAsync(fs => fs.AccountId == accountId) ?? throw new ArgumentException("Tài khoản không phải là FacilityStaff");
            var facilityId = staff.FacilityId;
            var facilityMembership = await facilityMembershipRepo.GetByIdAsync(facilityMembershipId) ?? throw new ArgumentException("Không tìm thấy gói membership cho cơ sở");

            // Nếu đã có active thì không tạo nữa
            var activeSubscription = await subscriptionRepo.GetAsync(s => s.FacilityId == facilityId && s.Status == true);
            if (activeSubscription != null)
            {
                return activeSubscription.SubscriptionId;
            }

            var newSubscription = new FacilityMembershipSubscription
            {
                FacilityId = facilityId,
                FacilityMembershipId = facilityMembershipId,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(facilityMembership.Duration),
                Status = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await subscriptionRepo.AddAsync(newSubscription);
            await _unitOfWork.SaveChangesAsync();
            return newSubscription.SubscriptionId;
        }

        private string GetPaymentStatusMessage(string status)
        {
            return status switch
            {
                "PAID" => "Thanh toán thành công",
                "CANCELLED" => "Thanh toán đã bị hủy",
                "PENDING" => "Đang chờ thanh toán",
                "FAILED" => "Thanh toán thất bại",
                _ => "Trạng thái không xác định"
            };
        }

        private string GetBaseUrl()
        {
            return _configuration["BaseUrl"] ?? "https://localhost:7000";
        }

        #endregion
    }
} 