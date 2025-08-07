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

                // Xác định loại transaction và tạo membership/subscription
                string transactionType;
                decimal amount = 0;
                string description = "";
                int? userMembershipId = null;
                int? facilityMembershipSubscriptionId = null;

                if (request.MembershipId.HasValue)
                {
                    transactionType = "User";
                    var result = await ProcessUserMembershipPayment(request);
                    amount = result.Amount;
                    description = result.Description;
                    userMembershipId = result.UserMembershipId;
                }
                else if (request.FacilityMembershipId.HasValue)
                {
                    transactionType = "Facility";
                    var result = await ProcessFacilityMembershipPayment(request);
                    amount = result.Amount;
                    description = result.Description;
                    facilityMembershipSubscriptionId = result.FacilityMembershipSubscriptionId;
                }
                else
                {
                    throw new ArgumentException("Phải có MembershipId hoặc FacilityMembershipId");
                }

                // Tạo mã giao dịch unique
                string orderCode = $"{DateTimeOffset.Now.ToUnixTimeMilliseconds()}_{request.AccountId}_{transactionType}";

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

                // Lưu transaction vào database với membership/subscription đã tạo
                var newTransaction = new Transaction
                {
                    UserMembershipId = userMembershipId,
                    FacilityMembershipSubscriptionId = facilityMembershipSubscriptionId,
                    TransactionType = transactionType,
                    Amount = amount,
                    PaymentMethod = "PAYOS",
                    TransactionCode = orderCode,
                    Description = description,
                    Status = "PENDING", // Trạng thái ban đầu
                    CreatedAt = DateOnly.FromDateTime(DateTime.UtcNow)
                };

                var transactionRepo = _unitOfWork.GetRepository<Transaction>();
                await transactionRepo.AddAsync(newTransaction);
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Tạo payment thành công. OrderCode: {OrderCode}, PaymentUrl: {PaymentUrl}, UserMembershipId: {UserMembershipId}, FacilityMembershipSubscriptionId: {FacilityMembershipSubscriptionId}", 
                    orderCode, createPayment.checkoutUrl, userMembershipId, facilityMembershipSubscriptionId);

                return new PaymentDetailResponseDTO
                {
                    PaymentUrl = createPayment.checkoutUrl,
                    OrderId = orderCode,
                    Amount = amount,
                    Status = "PENDING",
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

        public async Task<PaymentStatusDTO> CheckPaymentStatusAsync(string orderId)
        {
            try
            {
                _logger.LogInformation("Kiểm tra trạng thái payment cho OrderId: {OrderId}", orderId);

                var transactionRepo = _unitOfWork.GetRepository<Transaction>();
                var existingTransaction = await transactionRepo.GetAsync(t => t.TransactionCode == orderId);

                if (existingTransaction == null)
                {
                    throw new KeyNotFoundException($"Không tìm thấy giao dịch với mã {orderId}");
                }

                // Kiểm tra từ PayOS nếu chưa PAID
                if (!existingTransaction.PaymentMethod.Contains("PAID"))
                {
                    var paymentInfo = await _payOS.getPaymentLinkInformation(long.Parse(orderId.Split('_')[0]));
                    _logger.LogInformation("PayOS Status: {Status} cho OrderId: {OrderId}", paymentInfo.status, orderId);

                    if (paymentInfo.status.Equals("PAID", StringComparison.OrdinalIgnoreCase))
                    {
                        await ProcessPaymentWebhookAsync(orderId, "PAID", paymentInfo.amount);
                    }
                    else if (paymentInfo.status.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase))
                    {
                        await ProcessPaymentWebhookAsync(orderId, "CANCELLED", paymentInfo.amount);
                    }
                }

                // Reload transaction sau khi cập nhật
                existingTransaction = await transactionRepo.GetAsync(t => t.TransactionCode == orderId);

                return new PaymentStatusDTO
                {
                    Success = existingTransaction.Status == "PAID",
                    Status = existingTransaction.Status,
                    Message = GetPaymentStatusMessage(existingTransaction.Status),
                    Amount = existingTransaction.Amount,
                    PaidAt = existingTransaction.Status == "PAID" ? DateTime.UtcNow : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi kiểm tra trạng thái payment cho OrderId: {OrderId}", orderId);
                throw;
            }
        }

        public async Task<bool> ProcessPaymentWebhookAsync(string orderId, string status, decimal amount)
        {
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

                // Cập nhật transaction status
                existingTransaction.Status = status;
                existingTransaction.PaymentMethod = $"{existingTransaction.PaymentMethod}_{status}";
                existingTransaction.Amount = amount;
                transactionRepo.Update(existingTransaction);

                if (status.Equals("PAID", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Thanh toán thành công, cập nhật status membership cho OrderId: {OrderId}", orderId);
                    
                    // Cập nhật status của UserMembership hoặc FacilityMembershipSubscription
                    if (existingTransaction.UserMembershipId.HasValue)
                    {
                        await ActivateUserMembership(existingTransaction.UserMembershipId.Value);
                    }
                    else if (existingTransaction.FacilityMembershipSubscriptionId.HasValue)
                    {
                        await ActivateFacilityMembershipSubscription(existingTransaction.FacilityMembershipSubscriptionId.Value);
                    }
                }

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Xử lý webhook thành công cho OrderId: {OrderId}", orderId);
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

        private async Task<(decimal Amount, string Description, int? UserMembershipId)> ProcessUserMembershipPayment(PaymentRequestDTO request)
        {
            if (!request.MembershipId.HasValue)
            {
                throw new ArgumentException("MembershipId là bắt buộc cho UserMembership");
            }

            var accountRepo = _unitOfWork.GetRepository<Account>();
            var membershipRepo = _unitOfWork.GetRepository<Membership>();
            var userMembershipRepo = _unitOfWork.GetRepository<UserMembership>();

            // Kiểm tra account
            var account = await accountRepo.GetByIdAsync(request.AccountId);
            if (account == null)
            {
                throw new ArgumentException("Không tìm thấy tài khoản");
            }

            // Kiểm tra membership
            var membership = await membershipRepo.GetByIdAsync(request.MembershipId.Value);
            if (membership == null)
            {
                throw new ArgumentException("Không tìm thấy gói membership");
            }

            // ✅ VALIDATE: Kiểm tra user có membership active không (không được mua lại)
            var activeMembership = await userMembershipRepo.GetAsync(
                um => um.AccountId == request.AccountId && um.Status == true);
            if (activeMembership != null)
            {
                throw new InvalidOperationException($"Tài khoản đã có gói membership đang hoạt động. Gói hiện tại sẽ tự động gia hạn khi hết hạn vào {activeMembership.EndDate:dd/MM/yyyy}");
            }

            // ✅ VALIDATE: Kiểm tra không có UserMembership pending nào (status = false)
            var pendingMembership = await userMembershipRepo.GetAsync(
                um => um.AccountId == request.AccountId && um.Status == false);
            if (pendingMembership != null)
            {
                throw new InvalidOperationException($"Tài khoản đã có gói membership đang chờ thanh toán. Vui lòng hoàn tất thanh toán trước khi mua gói mới.");
            }

            // ✅ TẠO UserMembership với status = false (chờ thanh toán)
            var newUserMembership = new UserMembership
            {
                AccountId = request.AccountId,
                MembershipId = request.MembershipId.Value,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(membership.Duration),
                Status = false, // Chờ thanh toán
                LastRenewalDate = DateOnly.FromDateTime(DateTime.UtcNow)
            };

            await userMembershipRepo.AddAsync(newUserMembership);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Đã tạo UserMembership với ID: {UserMembershipId} cho AccountId: {AccountId}", 
                newUserMembership.UserMembershipId, request.AccountId);

            return (membership.Price, "Goi thanh vien", newUserMembership.UserMembershipId);
        }

        private async Task<(decimal Amount, string Description, int? FacilityMembershipSubscriptionId)> ProcessFacilityMembershipPayment(PaymentRequestDTO request)
        {
            if (!request.FacilityMembershipId.HasValue)
            {
                throw new ArgumentException("FacilityMembershipId là bắt buộc cho FacilityMembership");
            }

            // Lấy FacilityId từ FacilityStaff của AccountId
            var facilityStaffRepo = _unitOfWork.GetRepository<FacilityStaff>();
            var facilityStaff = await facilityStaffRepo.GetAsync(fs => fs.AccountId == request.AccountId);
            if (facilityStaff == null)
            {
                throw new ArgumentException("Tài khoản không phải là FacilityStaff");
            }

            var facilityId = facilityStaff.FacilityId;

            var accountRepo = _unitOfWork.GetRepository<Account>();
            var facilityRepo = _unitOfWork.GetRepository<VaccinationFacility>();
            var facilityMembershipRepo = _unitOfWork.GetRepository<FacilityMembership>();
            var subscriptionRepo = _unitOfWork.GetRepository<FacilityMembershipSubscription>();

            // Kiểm tra account
            var account = await accountRepo.GetByIdAsync(request.AccountId);
            if (account == null)
            {
                throw new ArgumentException("Không tìm thấy tài khoản");
            }

            // Kiểm tra facility
            var facility = await facilityRepo.GetByIdAsync(facilityId);
            if (facility == null)
            {
                throw new ArgumentException("Không tìm thấy cơ sở");
            }

            // Kiểm tra facility membership
            var facilityMembership = await facilityMembershipRepo.GetByIdAsync(request.FacilityMembershipId.Value);
            if (facilityMembership == null)
            {
                throw new ArgumentException("Không tìm thấy gói membership cho cơ sở");
            }

            // ✅ VALIDATE: Kiểm tra facility có subscription active không (không được mua lại)
            var activeSubscription = await subscriptionRepo.GetAsync(
                s => s.FacilityId == facilityId && s.Status == true);
            if (activeSubscription != null)
            {
                throw new InvalidOperationException($"Cơ sở đã có gói membership đang hoạt động. Gói hiện tại sẽ tự động gia hạn khi hết hạn vào {activeSubscription.EndDate:dd/MM/yyyy}");
            }

            // ✅ VALIDATE: Kiểm tra không có FacilityMembershipSubscription pending nào (status = false)
            var pendingSubscription = await subscriptionRepo.GetAsync(
                s => s.FacilityId == facilityId && s.Status == false);
            if (pendingSubscription != null)
            {
                throw new InvalidOperationException($"Cơ sở đã có gói membership đang chờ thanh toán. Vui lòng hoàn tất thanh toán trước khi mua gói mới.");
            }

            // ✅ TẠO FacilityMembershipSubscription với status = false (chờ thanh toán)
            var newSubscription = new FacilityMembershipSubscription
            {
                FacilityId = facilityId,
                FacilityMembershipId = request.FacilityMembershipId.Value,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(facilityMembership.Duration),
                Status = false, // Chờ thanh toán
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await subscriptionRepo.AddAsync(newSubscription);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Đã tạo FacilityMembershipSubscription với ID: {SubscriptionId} cho FacilityId: {FacilityId}", 
                newSubscription.SubscriptionId, facilityId);

            return (facilityMembership.Price, "Goi co so", newSubscription.SubscriptionId);
        }

        private async Task ActivateUserMembership(int userMembershipId)
        {
            _logger.LogInformation("Bắt đầu ActivateUserMembership cho UserMembershipId: {UserMembershipId}", userMembershipId);
            
            var userMembershipRepo = _unitOfWork.GetRepository<UserMembership>();
            var userMembership = await userMembershipRepo.GetAsync(um => um.UserMembershipId == userMembershipId);
            
            if (userMembership == null)
            {
                _logger.LogError("Không tìm thấy UserMembership với ID: {UserMembershipId}", userMembershipId);
                return;
            }

            // Cập nhật status thành true (đã thanh toán thành công)
            userMembership.Status = true;
            userMembershipRepo.Update(userMembership);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Đã cập nhật UserMembership với ID: {UserMembershipId} thành status = true", userMembershipId);
        }

        private async Task ActivateFacilityMembershipSubscription(int subscriptionId)
        {
            _logger.LogInformation("Bắt đầu ActivateFacilityMembershipSubscription cho SubscriptionId: {SubscriptionId}", subscriptionId);
            
            var subscriptionRepo = _unitOfWork.GetRepository<FacilityMembershipSubscription>();
            var subscription = await subscriptionRepo.GetAsync(s => s.SubscriptionId == subscriptionId);
            
            if (subscription == null)
            {
                _logger.LogError("Không tìm thấy FacilityMembershipSubscription với ID: {SubscriptionId}", subscriptionId);
                return;
            }

            // Cập nhật status thành true (đã thanh toán thành công)
            subscription.Status = true;
            subscriptionRepo.Update(subscription);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Đã cập nhật FacilityMembershipSubscription với ID: {SubscriptionId} thành status = true", subscriptionId);
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