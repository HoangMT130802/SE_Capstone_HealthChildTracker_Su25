namespace Contracts.DTOs.VaccinationFacilityPaymentAccount
{
    /// <summary>
    /// Response cho facility payment - đơn giản hóa
    /// </summary>
    public class FacilityPaymentResponseDTO
    {
        /// <summary>
        /// URL thanh toán PayOS
        /// </summary>
        public string PaymentUrl { get; set; } = string.Empty;

        /// <summary>
        /// Mã đơn hàng PayOS để tracking
        /// </summary>
        public string OrderCode { get; set; } = string.Empty;

        /// <summary>
        /// Số tiền thanh toán
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Trạng thái thanh toán (luôn là PENDING khi tạo mới)
        /// </summary>
        public string Status { get; set; } = "PENDING";

        /// <summary>
        /// ID Appointment được thanh toán
        /// </summary>
        public int AppointmentId { get; set; }

        /// <summary>
        /// Loại thanh toán được tự động phát hiện (ORDER hoặc INDIVIDUAL_VACCINE)
        /// </summary>
        public string PaymentType { get; set; } = string.Empty;

        /// <summary>
        /// Mô tả thanh toán
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// ID Order (chỉ có khi PaymentType = ORDER)
        /// </summary>
        public int? OrderId { get; set; }

        /// <summary>
        /// ID Transaction được tạo trong hệ thống
        /// </summary>
        public int TransactionId { get; set; }
    }
}

