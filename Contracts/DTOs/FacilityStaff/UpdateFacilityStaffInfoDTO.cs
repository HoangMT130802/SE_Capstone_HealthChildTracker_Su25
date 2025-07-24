using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.FacilityStaff
{
    public class UpdateFacilityStaffInfoDTO
    {
        [Required(ErrorMessage = "ID Staff là bắt buộc")]
        public int StaffId { get; set; }

        [Required(ErrorMessage = "Họ và tên là bắt buộc")]
        [StringLength(100, ErrorMessage = "Họ và tên không được vượt quá 100 ký tự")]
        public string FullName { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Chức vụ là bắt buộc")]
        [StringLength(50, ErrorMessage = "Chức vụ không được vượt quá 50 ký tự")]
        public string Position { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string? Description { get; set; }

        public bool Status { get; set; } = true;

        // ✅ DoctorProfile fields (chỉ dùng khi Position = "Doctor")
        public int? Age { get; set; }
        
        [StringLength(200, ErrorMessage = "Chuyên khoa không được vượt quá 200 ký tự")]
        public string? Specialization { get; set; }
        
        [StringLength(500, ErrorMessage = "Chứng chỉ không được vượt quá 500 ký tự")]
        public string? Certifications { get; set; }
        
        [StringLength(200, ErrorMessage = "Trường đại học không được vượt quá 200 ký tự")]
        public string? University { get; set; }
        
        [StringLength(1000, ErrorMessage = "Tiểu sử không được vượt quá 1000 ký tự")]
        public string? Bio { get; set; }
    }
} 