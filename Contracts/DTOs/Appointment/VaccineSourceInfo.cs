namespace Contracts.DTOs.Appointment
{
    /// <summary>
    /// Thông tin về nguồn vaccine (từ order hoặc mua lẻ)
    /// </summary>
    public class VaccineSourceInfo
    {
        /// <summary>
        /// Loại nguồn: "Order" hoặc "Individual"
        /// </summary>
        public string SourceType { get; set; } = string.Empty;
        
        /// <summary>
        /// ID của order (nếu từ order)
        /// </summary>
        public int? OrderId { get; set; }
        
        /// <summary>
        /// ID của OrderDetail (nếu từ order)
        /// </summary>
        public int? OrderDetailId { get; set; }
        
        /// <summary>
        /// Tên gói vaccine (nếu từ order)
        /// </summary>
        public string? PackageName { get; set; }
        
        /// <summary>
        /// Số lượng còn lại trong order
        /// </summary>
        public int RemainingQuantity { get; set; }
        
        /// <summary>
        /// Đã trả tiền chưa
        /// </summary>
        public bool IsPaid { get; set; }
        
        /// <summary>
        /// Giá vaccine (miễn phí nếu từ order đã trả tiền)
        /// </summary>
        public decimal Price { get; set; }
        
        /// <summary>
        /// Có ưu tiên sử dụng không
        /// </summary>
        public bool IsPriority { get; set; }
    }
}
