using Contracts.DTOs.Transaction;

namespace Services.Interfaces
{
    public interface IPaymentService
    {
        /// <summary>
        /// Tạo payment cho UserMembership hoặc FacilityMembership
        /// </summary>
        Task<PaymentDetailResponseDTO> CreatePaymentAsync(PaymentRequestDTO request);
        
        /// <summary>
        /// Kiểm tra trạng thái thanh toán
        /// </summary>
        Task<PaymentStatusDTO> CheckPaymentStatusAsync(string orderId);
        
        /// <summary>
        /// Xử lý webhook từ PayOS
        /// </summary>
        Task<bool> ProcessPaymentWebhookAsync(string orderId, string status, decimal amount);
    }
} 