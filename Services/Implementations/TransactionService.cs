using AutoMapper;
using BusinessLogic.DTOs.Transaction;
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
                var transactionRepo = _unitOfWork.GetRepository<Transaction>();
                var transactions = await transactionRepo.GetAllAsync(
                    includeProperties: "UserMembership,UserMembership.Membership,UserMembership.User"
                );

                // Sắp xếp theo thời gian mới nhất
                var sortedTransactions = transactions.OrderByDescending(t => t.CreatedAt);

                return _mapper.Map<IEnumerable<TransactionDTO>>(sortedTransactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách giao dịch");
                throw;
            }
        }

        public async Task<IEnumerable<TransactionDTO>> GetTransactionsByUserIdAsync(int userId)
        {
            try
            {
                var transactionRepo = _unitOfWork.GetRepository<Transaction>();
                var transactions = await transactionRepo.FindAsync(
                    t => t.UserId == userId,
                    includeProperties: "UserMembership,UserMembership.Membership"
                );

                // Sắp xếp theo thời gian mới nhất
                var sortedTransactions = transactions.OrderByDescending(t => t.CreatedAt);

                return _mapper.Map<IEnumerable<TransactionDTO>>(sortedTransactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách giao dịch của user {UserId}", userId);
                throw;
            }
        }
    }
}
