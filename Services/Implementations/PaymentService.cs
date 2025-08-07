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
                _logger.LogInformation("Bắt đầu tạo payment cho Account {AccountId}", request.AccountId);

                // Xác định loại transaction
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

                // Lưu transaction vào database (chỉ lưu transaction, không tạo membership/subscription)
                // Lưu AccountId và MembershipId trong Description để sử dụng sau
                var membershipId = request.MembershipId ?? request.FacilityMembershipId;
                var transactionDescription = $"{description}|AccountId:{request.AccountId}|MembershipId:{membershipId}";
                
                var newTransaction = new Transaction
                {
                    UserMembershipId = null, // Sẽ cập nhật khi thanh toán thành công
                    FacilityMembershipSubscriptionId = null, // Sẽ cập nhật khi thanh toán thành công
                    TransactionType = transactionType,
                    Amount = amount,
                    PaymentMethod = "PAYOS", // Tự động điền
                    TransactionCode = orderCode,
                    Description = transactionDescription, // Lưu thông tin cần thiết
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
                    ReturnUrl = $"{GetBaseUrl()}/payment/success?orderId={orderCode}",
                    CancelUrl = $"{GetBaseUrl()}/payment/cancel"
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
                    _logger.LogInformation("Thanh toán thành công, bắt đầu xử lý membership cho OrderId: {OrderId}", orderId);
                    
                    // Parse MembershipId từ Description để xác định loại payment
                    var descriptionParts = existingTransaction.Description.Split('|');
                    _logger.LogInformation("Transaction Description: {Description}", existingTransaction.Description);
                    _logger.LogInformation("Description Parts: {Parts}", string.Join(", ", descriptionParts));
                    
                    var membershipIdPart = descriptionParts.FirstOrDefault(p => p.StartsWith("MembershipId:"));
                    _logger.LogInformation("MembershipId Part: {MembershipIdPart}", membershipIdPart);
                    
                    var membershipId = int.Parse(membershipIdPart?.Split(':')[1] ?? "0");
                    _logger.LogInformation("Parsed MembershipId: {MembershipId}", membershipId);
                    
                    if (membershipId > 0)
                    {
                        // Kiểm tra xem là UserMembership hay FacilityMembership
                        var membershipRepo = _unitOfWork.GetRepository<Membership>();
                        var membership = await membershipRepo.GetAsync(m => m.MembershipId == membershipId);
                        
                        if (membership != null)
                        {
                            _logger.LogInformation("Tìm thấy Membership, xử lý UserMembership cho MembershipId: {MembershipId}", membershipId);
                            // UserMembership
                            await ActivateUserMembership(membershipId, orderId);
                        }
                        else
                        {
                            _logger.LogInformation("Không tìm thấy Membership, xử lý FacilityMembership cho MembershipId: {MembershipId}", membershipId);
                            // FacilityMembership
                            await ActivateFacilityMembershipSubscription(membershipId, orderId);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("MembershipId không hợp lệ: {MembershipId}", membershipId);
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

            // ✅ CHỈ TRẢ VỀ THÔNG TIN, KHÔNG TẠO UserMembership - sẽ tạo khi thanh toán thành công
            return (membership.Price, "Goi thanh vien", null);
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

            // ✅ CHỈ TRẢ VỀ THÔNG TIN, KHÔNG TẠO FacilityMembershipSubscription - sẽ tạo khi thanh toán thành công
            return (facilityMembership.Price, "Goi co so", null);
        }

        private async Task ActivateUserMembership(int membershipId, string orderId)
        {
            _logger.LogInformation("Bắt đầu ActivateUserMembership cho MembershipId: {MembershipId}, OrderId: {OrderId}", membershipId, orderId);
            
            // Lấy thông tin transaction để biết AccountId
            var transactionRepo = _unitOfWork.GetRepository<Transaction>();
            var transaction = await transactionRepo.GetAsync(t => t.TransactionCode == orderId);
            
            if (transaction == null)
            {
                _logger.LogError("Không tìm thấy transaction cho OrderId: {OrderId}", orderId);
                return;
            }

            // Parse AccountId từ Description
            var descriptionParts = transaction.Description.Split('|');
            var accountIdPart = descriptionParts.FirstOrDefault(p => p.StartsWith("AccountId:"));
            var accountId = int.Parse(accountIdPart?.Split(':')[1] ?? "0");
            
            if (accountId == 0)
            {
                _logger.LogError("Không thể lấy AccountId từ transaction description cho OrderId: {OrderId}", orderId);
                return;
            }
            
            // Lấy thông tin membership
            var membershipRepo = _unitOfWork.GetRepository<Membership>();
            var membership = await membershipRepo.GetAsync(m => m.MembershipId == membershipId);
            
            if (membership == null)
            {
                _logger.LogError("Không tìm thấy membership với ID: {MembershipId}", membershipId);
                return;
            }

            // Tạo UserMembership mới với status = true (đã thanh toán thành công)
            var userMembershipRepo = _unitOfWork.GetRepository<UserMembership>();
            var newUserMembership = new UserMembership
            {
                AccountId = accountId,
                MembershipId = membershipId,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(membership.Duration),
                Status = true, // Đã thanh toán thành công
                LastRenewalDate = DateOnly.FromDateTime(DateTime.UtcNow)
            };

            await userMembershipRepo.AddAsync(newUserMembership);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Đã tạo UserMembership với ID: {UserMembershipId}", newUserMembership.UserMembershipId);

            // Cập nhật transaction với UserMembershipId
            transaction.UserMembershipId = newUserMembership.UserMembershipId;
            transactionRepo.Update(transaction);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Đã cập nhật Transaction với UserMembershipId: {UserMembershipId} cho OrderId: {OrderId}", 
                newUserMembership.UserMembershipId, orderId);
        }

        private async Task ActivateFacilityMembershipSubscription(int facilityMembershipId, string orderId)
        {
            // Lấy thông tin transaction để biết AccountId
            var transactionRepo = _unitOfWork.GetRepository<Transaction>();
            var transaction = await transactionRepo.GetAsync(t => t.TransactionCode == orderId);
            
            if (transaction == null)
            {
                _logger.LogError("Không tìm thấy transaction cho OrderId: {OrderId}", orderId);
                return;
            }

            // Parse AccountId từ Description
            var descriptionParts = transaction.Description.Split('|');
            var accountIdPart = descriptionParts.FirstOrDefault(p => p.StartsWith("AccountId:"));
            var accountId = int.Parse(accountIdPart?.Split(':')[1] ?? "0");
            
            if (accountId == 0)
            {
                _logger.LogError("Không thể lấy AccountId từ transaction description cho OrderId: {OrderId}", orderId);
                return;
            }

            // Lấy FacilityId từ FacilityStaff của AccountId
            var facilityStaffRepo = _unitOfWork.GetRepository<FacilityStaff>();
            var facilityStaff = await facilityStaffRepo.GetAsync(fs => fs.AccountId == accountId);
            if (facilityStaff == null)
            {
                _logger.LogError("Không tìm thấy FacilityStaff cho AccountId: {AccountId}", accountId);
                return;
            }

            // Lấy thông tin facility membership
            var facilityMembershipRepo = _unitOfWork.GetRepository<FacilityMembership>();
            var facilityMembership = await facilityMembershipRepo.GetAsync(fm => fm.FacilityMembershipId == facilityMembershipId);
            
            if (facilityMembership == null)
            {
                _logger.LogError("Không tìm thấy facility membership với ID: {FacilityMembershipId}", facilityMembershipId);
                return;
            }

            // Tạo FacilityMembershipSubscription mới với status = true (đã thanh toán thành công)
            var subscriptionRepo = _unitOfWork.GetRepository<FacilityMembershipSubscription>();
            var newSubscription = new FacilityMembershipSubscription
            {
                FacilityId = facilityStaff.FacilityId,
                FacilityMembershipId = facilityMembershipId,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(facilityMembership.Duration),
                Status = true, // Đã thanh toán thành công
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await subscriptionRepo.AddAsync(newSubscription);
            await _unitOfWork.SaveChangesAsync();

            // Cập nhật transaction với FacilityMembershipSubscriptionId
            transaction.FacilityMembershipSubscriptionId = newSubscription.SubscriptionId;
            transactionRepo.Update(transaction);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Created and activated FacilityMembershipSubscription {SubscriptionId} cho OrderId: {OrderId}", 
                newSubscription.SubscriptionId, orderId);
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