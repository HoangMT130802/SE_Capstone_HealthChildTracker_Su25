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
        /// Kiểm tra trạng thái thanh toán (poll PayOS) và đồng bộ DB, kích hoạt/hủy membership/subscription nếu cần
        /// </summary>
        Task<PaymentStatusDTO> CheckPaymentStatusAsync(string orderId);
    }
} 