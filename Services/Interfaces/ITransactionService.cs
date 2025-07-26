using Contracts.DTOs.Transaction;

namespace Services.Interfaces
{
    public interface ITransactionService
    {
        /// <summary>
        /// Lấy tất cả transactions (Admin only)
        /// </summary>
        Task<IEnumerable<TransactionDTO>> GetAllTransactionsAsync();
        
        /// <summary>
        /// Lấy transactions theo AccountId
        /// </summary>
        Task<IEnumerable<TransactionDTO>> GetTransactionsByAccountIdAsync(int accountId);
        
        /// <summary>
        /// Lấy transactions theo FacilityId
        /// </summary>
        Task<IEnumerable<TransactionDTO>> GetTransactionsByFacilityIdAsync(int facilityId);
        
        /// <summary>
        /// Lấy transaction theo ID
        /// </summary>
        Task<TransactionDTO> GetTransactionByIdAsync(int transactionId);
        
        /// <summary>
        /// Tạo transaction mới
        /// </summary>
        Task<TransactionDTO> CreateTransactionAsync(CreateTransactionDTO createDto);
        
        /// <summary>
        /// Cập nhật trạng thái transaction
        /// </summary>
        Task<bool> UpdateTransactionStatusAsync(string transactionCode, string status);
    }
} 