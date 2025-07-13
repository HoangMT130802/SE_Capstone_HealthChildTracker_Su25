using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.Appointment
{
    /// <summary>
    /// DTO cho các bộ lọc tìm kiếm cơ sở và lịch hẹn
    /// </summary>
    public class AppointmentSearchFiltersDTO
    {
        /// <summary>
        /// ID bệnh cần điều trị
        /// </summary>
        [Required(ErrorMessage = "ID bệnh là bắt buộc")]
        public int DiseaseId { get; set; }

        /// <summary>
        /// Vị trí địa lý người dùng (để tính khoảng cách)
        /// </summary>
        public UserLocationDTO? UserLocation { get; set; }

        /// <summary>
        /// Bán kính tìm kiếm (km)
        /// </summary>
        [Range(1, 100, ErrorMessage = "Bán kính tìm kiếm từ 1-100km")]
        public double? SearchRadius { get; set; } = 20;

        /// <summary>
        /// Khoảng giá tối thiểu
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Giá tối thiểu phải >= 0")]
        public decimal? MinPrice { get; set; }

        /// <summary>
        /// Khoảng giá tối đa
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Giá tối đa phải >= 0")]
        public decimal? MaxPrice { get; set; }

        /// <summary>
        /// Chỉ hiển thị cơ sở có gói vaccine
        /// </summary>
        public bool? HasPackagesOnly { get; set; }

        /// <summary>
        /// Ngày bắt đầu tìm lịch
        /// </summary>
        public DateOnly? FromDate { get; set; }

        /// <summary>
        /// Ngày kết thúc tìm lịch
        /// </summary>
        public DateOnly? ToDate { get; set; }

        /// <summary>
        /// Khung giờ ưu tiên
        /// </summary>
        public List<string>? PreferredTimeSlots { get; set; }

        /// <summary>
        /// Sắp xếp theo
        /// </summary>
        public FacilitySortBy SortBy { get; set; } = FacilitySortBy.Distance;

        /// <summary>
        /// Thứ tự sắp xếp
        /// </summary>
        public SortOrder SortOrder { get; set; } = SortOrder.Ascending;
    }

    /// <summary>
    /// DTO cho vị trí người dùng
    /// </summary>
    public class UserLocationDTO
    {
        [Required]
        [Range(-90, 90, ErrorMessage = "Latitude phải trong khoảng -90 đến 90")]
        public double Latitude { get; set; }

        [Required]
        [Range(-180, 180, ErrorMessage = "Longitude phải trong khoảng -180 đến 180")]
        public double Longitude { get; set; }

        /// <summary>
        /// Địa chỉ mô tả (tùy chọn)
        /// </summary>
        public string? Address { get; set; }
    }

    /// <summary>
    /// Các tiêu chí sắp xếp cơ sở
    /// </summary>
    public enum FacilitySortBy
    {
        Distance,       // Khoảng cách
        Price,          // Giá
        Name,           // Tên cơ sở
        Rating,         // Đánh giá
        AvailableSlots  // Số lịch trống
    }

    /// <summary>
    /// Thứ tự sắp xếp
    /// </summary>
    public enum SortOrder
    {
        Ascending,
        Descending
    }
} 