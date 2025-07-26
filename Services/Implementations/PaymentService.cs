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

        public async Task<PaymentResponseDTO> CreatePaymentAsync(PaymentRequestDTO request)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("Bắt đầu tạo payment cho Account {AccountId}, Type: {TransactionType}", 
                    request.AccountId, request.TransactionType);

                decimal amount = 0;
                string description = "";
                int? userMembershipId = null;
                int? facilityMembershipSubscriptionId = null;

                if (request.TransactionType == "UserMembership")
                {
                    var result = await ProcessUserMembershipPayment(request);
                    amount = result.Amount;
                    description = result.Description;
                    userMembershipId = result.UserMembershipId;
                }
                else if (request.TransactionType == "FacilityMembership")
                {
                    var result = await ProcessFacilityMembershipPayment(request);
                    amount = result.Amount;
                    description = result.Description;
                    facilityMembershipSubscriptionId = result.FacilityMembershipSubscriptionId;
                }
                else
                {
                    throw new ArgumentException("TransactionType không hợp lệ");
                }

                // Tạo mã giao dịch unique
                string orderCode = $"{DateTimeOffset.Now.ToUnixTimeMilliseconds()}_{request.AccountId}_{request.TransactionType}";

                // Tạo payment link với PayOS
                var paymentData = new PaymentData(
                    long.Parse(orderCode.Split('_')[0]),
                    (int)amount,
                    description,
                    new List<ItemData>(),
                    $"{GetBaseUrl()}/payment/cancel",
                    $"{GetBaseUrl()}/payment/success?orderId={orderCode}",
                    null
                );

                var createPayment = await _payOS.createPaymentLink(paymentData);

                // Lưu transaction vào database
                var newTransaction = new Transaction
                {
                    UserMembershipId = userMembershipId,
                    FacilityMembershipSubscriptionId = facilityMembershipSubscriptionId,
                    TransactionType = request.TransactionType,
                    Amount = amount,
                    PaymentMethod = request.PaymentMethod ?? "PAYOS",
                    TransactionCode = orderCode,
                    Description = description,
                    CreatedAt = DateOnly.FromDateTime(DateTime.UtcNow)
                };

                var transactionRepo = _unitOfWork.GetRepository<Transaction>();
                await transactionRepo.AddAsync(newTransaction);
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Tạo payment thành công. OrderCode: {OrderCode}, PaymentUrl: {PaymentUrl}", 
                    orderCode, createPayment.checkoutUrl);

                return new PaymentResponseDTO
                {
                    PaymentUrl = createPayment.checkoutUrl,
                    OrderId = orderCode,
                    Amount = amount,
                    Status = "PENDING",
                    Message = "Payment link đã được tạo thành công"
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
                    Success = existingTransaction.PaymentMethod.Contains("PAID"),
                    Status = existingTransaction.PaymentMethod.Contains("PAID") ? "PAID" : 
                             existingTransaction.PaymentMethod.Contains("CANCELLED") ? "CANCELLED" : "PENDING",
                    Message = GetPaymentStatusMessage(existingTransaction.PaymentMethod),
                    Amount = existingTransaction.Amount,
                    PaidAt = existingTransaction.PaymentMethod.Contains("PAID") ? DateTime.UtcNow : null
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
                existingTransaction.PaymentMethod = $"{existingTransaction.PaymentMethod}_{status}";
                existingTransaction.Amount = amount;
                transactionRepo.Update(existingTransaction);

                if (status.Equals("PAID", StringComparison.OrdinalIgnoreCase))
                {
                    // Activate UserMembership hoặc FacilityMembershipSubscription
                    if (existingTransaction.UserMembershipId.HasValue)
                    {
                        await ActivateUserMembership(existingTransaction.UserMembershipId.Value, orderId);
                    }
                    else if (existingTransaction.FacilityMembershipSubscriptionId.HasValue)
                    {
                        await ActivateFacilityMembershipSubscription(existingTransaction.FacilityMembershipSubscriptionId.Value, orderId);
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

            // Kiểm tra user có membership active không
            var activeMembership = await userMembershipRepo.GetAsync(
                um => um.AccountId == request.AccountId && um.Status == true);
            if (activeMembership != null)
            {
                throw new InvalidOperationException("Tài khoản đã có membership đang hoạt động");
            }

            // Tạo UserMembership mới với trạng thái false (chờ thanh toán)
            var newUserMembership = new UserMembership
            {
                AccountId = request.AccountId,
                MembershipId = request.MembershipId.Value,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(membership.Duration),
                Status = false, // Chờ thanh toán
                RemainingConsultations = 0, // Sẽ cập nhật sau khi thanh toán
                LastRenewalDate = DateOnly.FromDateTime(DateTime.UtcNow)
            };

            await userMembershipRepo.AddAsync(newUserMembership);
            await _unitOfWork.SaveChangesAsync();

            return (membership.Price, $"Thanh toán gói {membership.Name}", newUserMembership.UserMembershipId);
        }

        private async Task<(decimal Amount, string Description, int? FacilityMembershipSubscriptionId)> ProcessFacilityMembershipPayment(PaymentRequestDTO request)
        {
            if (!request.FacilityMembershipId.HasValue || !request.FacilityId.HasValue)
            {
                throw new ArgumentException("FacilityMembershipId và FacilityId là bắt buộc cho FacilityMembership");
            }

            var facilityRepo = _unitOfWork.GetRepository<VaccinationFacility>();
            var facilityMembershipRepo = _unitOfWork.GetRepository<FacilityMembership>();
            var subscriptionRepo = _unitOfWork.GetRepository<FacilityMembershipSubscription>();

            // Kiểm tra facility
            var facility = await facilityRepo.GetByIdAsync(request.FacilityId.Value);
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

            // Kiểm tra facility có subscription active không
            var activeSubscription = await subscriptionRepo.GetAsync(
                s => s.FacilityId == request.FacilityId.Value && s.Status == true);
            if (activeSubscription != null)
            {
                throw new InvalidOperationException("Cơ sở đã có subscription đang hoạt động");
            }

            // Tạo FacilityMembershipSubscription mới
            var newSubscription = new FacilityMembershipSubscription
            {
                FacilityId = request.FacilityId.Value,
                FacilityMembershipId = request.FacilityMembershipId.Value,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(facilityMembership.Duration),
                Status = false, // Chờ thanh toán
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await subscriptionRepo.AddAsync(newSubscription);
            await _unitOfWork.SaveChangesAsync();

            return (facilityMembership.Price, $"Thanh toán gói {facilityMembership.Name} cho {facility.FacilityName}", newSubscription.SubscriptionId);
        }

        private async Task ActivateUserMembership(int userMembershipId, string orderId)
        {
            var userMembershipRepo = _unitOfWork.GetRepository<UserMembership>();
            var userMembership = await userMembershipRepo.GetAsync(um => um.UserMembershipId == userMembershipId, "Membership");

            if (userMembership != null)
            {
                userMembership.Status = true;
                userMembership.RemainingConsultations = 1000; // Set default hoặc từ membership
                userMembershipRepo.Update(userMembership);

                _logger.LogInformation("Activated UserMembership {UserMembershipId} cho OrderId: {OrderId}", 
                    userMembershipId, orderId);
            }
        }

        private async Task ActivateFacilityMembershipSubscription(int subscriptionId, string orderId)
        {
            var subscriptionRepo = _unitOfWork.GetRepository<FacilityMembershipSubscription>();
            var subscription = await subscriptionRepo.GetByIdAsync(subscriptionId);

            if (subscription != null)
            {
                subscription.Status = true;
                subscription.UpdatedAt = DateTime.UtcNow;
                subscriptionRepo.Update(subscription);

                _logger.LogInformation("Activated FacilityMembershipSubscription {SubscriptionId} cho OrderId: {OrderId}", 
                    subscriptionId, orderId);
            }
        }

        private string GetPaymentStatusMessage(string paymentMethod)
        {
            if (paymentMethod.Contains("PAID"))
                return "Thanh toán thành công";
            else if (paymentMethod.Contains("CANCELLED"))
                return "Thanh toán đã bị hủy";
            else
                return "Đang chờ thanh toán";
        }

        private string GetBaseUrl()
        {
            return _configuration["BaseUrl"] ?? "https://localhost:7000";
        }

        #endregion
    }
} 