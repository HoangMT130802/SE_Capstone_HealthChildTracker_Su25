using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.UserMembership
{
    public class GuestSubscribeMembershipDTO
    {
        [Required(ErrorMessage = "MembershipId không được để trống")]
        public int MembershipId { get; set; }
        
        // Thông tin cá nhân cho Guest (để tạo Member record)
        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(255, ErrorMessage = "Họ tên không được quá 255 ký tự")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [StringLength(20, ErrorMessage = "Số điện thoại không được quá 20 ký tự")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string PhoneNumber { get; set; }

        [StringLength(500, ErrorMessage = "Địa chỉ không được quá 500 ký tự")]
        public string Address { get; set; }
    }
} 