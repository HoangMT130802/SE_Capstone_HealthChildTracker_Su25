using AutoMapper;
using Contracts.DTOs.Transaction;
using Microsoft.Extensions.Logging;
using Repositories.Entities;
using Repositories.Interfaces;
using Services.Interfaces;

namespace Services.Implementations
{
    public class TransactionService : ITransactionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<TransactionService> _logger;

        public TransactionService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<TransactionService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<TransactionDTO>> GetAllTransactionsAsync()
        {
            try
            {
                _logger.LogInformation("Lấy tất cả transactions");

                var transactionRepo = _unitOfWork.GetRepository<Transaction>();
                var transactions = await transactionRepo.GetAllAsync(
                    "UserMembership,UserMembership.Account,UserMembership.Membership," +
                    "FacilityMembershipSubscription,FacilityMembershipSubscription.Facility," +
                    "FacilityMembershipSubscription.FacilityMembership");

                var sortedTransactions = transactions.OrderByDescending(t => t.CreatedAt);
                return _mapper.Map<IEnumerable<TransactionDTO>>(sortedTransactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy tất cả transactions");
                throw;
            }
        }

        public async Task<IEnumerable<TransactionDTO>> GetTransactionsByAccountIdAsync(int accountId)
        {
            try
            {
                _logger.LogInformation("Lấy transactions cho AccountId: {AccountId}", accountId);

                var transactionRepo = _unitOfWork.GetRepository<Transaction>();
                var transactions = await transactionRepo.FindAsync(
                    t => t.UserMembership.AccountId == accountId,
                    "UserMembership,UserMembership.Account,UserMembership.Membership");

                var sortedTransactions = transactions.OrderByDescending(t => t.CreatedAt);
                return _mapper.Map<IEnumerable<TransactionDTO>>(sortedTransactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy transactions cho AccountId: {AccountId}", accountId);
                throw;
            }
        }

        public async Task<IEnumerable<TransactionDTO>> GetTransactionsByFacilityIdAsync(int facilityId)
        {
            try
            {
                _logger.LogInformation("Lấy transactions cho FacilityId: {FacilityId}", facilityId);

                var transactionRepo = _unitOfWork.GetRepository<Transaction>();
                var transactions = await transactionRepo.FindAsync(
                    t => t.FacilityMembershipSubscription.FacilityId == facilityId,
                    "FacilityMembershipSubscription,FacilityMembershipSubscription.Facility," +
                    "FacilityMembershipSubscription.FacilityMembership");

                var sortedTransactions = transactions.OrderByDescending(t => t.CreatedAt);
                return _mapper.Map<IEnumerable<TransactionDTO>>(sortedTransactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy transactions cho FacilityId: {FacilityId}", facilityId);
                throw;
            }
        }

        public async Task<TransactionDTO> GetTransactionByIdAsync(int transactionId)
        {
            try
            {
                _logger.LogInformation("Lấy transaction với ID: {TransactionId}", transactionId);

                var transactionRepo = _unitOfWork.GetRepository<Transaction>();
                var transaction = await transactionRepo.GetAsync(
                    t => t.TransactionId == transactionId,
                    "UserMembership,UserMembership.Account,UserMembership.Membership," +
                    "FacilityMembershipSubscription,FacilityMembershipSubscription.Facility," +
                    "FacilityMembershipSubscription.FacilityMembership");

                if (transaction == null)
                {
                    throw new KeyNotFoundException($"Không tìm thấy transaction với ID: {transactionId}");
                }

                return _mapper.Map<TransactionDTO>(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy transaction với ID: {TransactionId}", transactionId);
                throw;
            }
        }

        public async Task<TransactionDTO> CreateTransactionAsync(CreateTransactionDTO createDto)
        {
            try
            {
                _logger.LogInformation("Tạo transaction mới với TransactionCode: {TransactionCode}", createDto.TransactionCode);

                var transactionRepo = _unitOfWork.GetRepository<Transaction>();
                
                // Kiểm tra TransactionCode đã tồn tại chưa
                var existingTransaction = await transactionRepo.GetAsync(t => t.TransactionCode == createDto.TransactionCode);
                if (existingTransaction != null)
                {
                    throw new InvalidOperationException($"TransactionCode {createDto.TransactionCode} đã tồn tại");
                }

                var transaction = _mapper.Map<Transaction>(createDto);
                transaction.CreatedAt = DateOnly.FromDateTime(DateTime.UtcNow);
                await transactionRepo.AddAsync(transaction);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Tạo transaction thành công với ID: {TransactionId}, Status: {Status}", transaction.TransactionId, transaction.Status);
                return _mapper.Map<TransactionDTO>(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo transaction với TransactionCode: {TransactionCode}", createDto.TransactionCode);
                throw;
            }
        }

        public async Task<bool> UpdateTransactionStatusAsync(string transactionCode, string status)
        {
            try
            {
                _logger.LogInformation("Cập nhật status cho TransactionCode: {TransactionCode} -> {Status}", transactionCode, status);

                var transactionRepo = _unitOfWork.GetRepository<Transaction>();
                var transaction = await transactionRepo.GetAsync(t => t.TransactionCode == transactionCode);

                if (transaction == null)
                {
                    throw new KeyNotFoundException($"Không tìm thấy transaction với TransactionCode: {transactionCode}");
                }

                // Cập nhật Status
                if (transaction.Status != status)
                {
                    transaction.Status = status;
                    transactionRepo.Update(transaction);
                    await _unitOfWork.SaveChangesAsync();

                    _logger.LogInformation("Cập nhật status thành công cho TransactionCode: {TransactionCode} -> {Status}", transactionCode, status);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật status cho TransactionCode: {TransactionCode}", transactionCode);
                throw;
            }
        }
    }
} 