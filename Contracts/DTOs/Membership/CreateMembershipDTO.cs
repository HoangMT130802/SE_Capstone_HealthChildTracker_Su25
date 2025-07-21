using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.Membership
{
    public class CreateMembershipDTO
    {
        [Required(ErrorMessage = "Tên gói membership không được để trống")]
        [StringLength(100, ErrorMessage = "Tên gói membership không được quá 100 ký tự")]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "Mô tả không được quá 500 ký tự")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Thời hạn không được để trống")]
        [Range(1, 120, ErrorMessage = "Thời hạn phải từ 1 đến 120 tháng")]
        public int Duration { get; set; } // Thời hạn (tháng)

        [Required(ErrorMessage = "Giá không được để trống")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        public decimal Price { get; set; }

        [StringLength(1000, ErrorMessage = "Quyền lợi không được quá 1000 ký tự")]
        public string Benefits { get; set; }

        public bool Status { get; set; } = true; // Mặc định là active
    }
} 