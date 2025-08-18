namespace Contracts.DTOs.VaccinationFacilityPaymentAccount
{
    /// <summary>
    /// Response cho facility payment
    /// </summary>
    public class FacilityPaymentResponseDTO
    {
        /// <summary>
        /// URL thanh toán PayOS
        /// </summary>
        public string PaymentUrl { get; set; } = string.Empty;

        /// <summary>
        /// Mã đơn hàng PayOS
        /// </summary>
        public string OrderCode { get; set; } = string.Empty;

        /// <summary>
        /// Số tiền thanh toán
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Trạng thái thanh toán
        /// </summary>
        public string Status { get; set; } = "PENDING";

        /// <summary>
        /// URL trả về khi thành công
        /// </summary>
        public string ReturnUrl { get; set; } = string.Empty;

        /// <summary>
        /// URL trả về khi hủy
        /// </summary>
        public string CancelUrl { get; set; } = string.Empty;

        /// <summary>
        /// ID Order được tạo (nếu có)
        /// </summary>
        public int? OrderId { get; set; }

        /// <summary>
        /// ID Appointment
        /// </summary>
        public int AppointmentId { get; set; }

        /// <summary>
        /// Loại thanh toán
        /// </summary>
        public string PaymentType { get; set; } = string.Empty;

        /// <summary>
        /// ID Transaction được tạo
        /// </summary>
        public int TransactionId { get; set; }

        /// <summary>
        /// Mô tả thanh toán
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }
}
