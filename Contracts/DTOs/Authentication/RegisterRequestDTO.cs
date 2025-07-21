using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.Authentication
{
    public class RegisterRequestDTO
    {
        [Required(ErrorMessage = "AccountName không được để trống")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "AccountName phải từ 3-50 ký tự")]
        public string AccountName { get; set; }

        [Required(ErrorMessage = "Password không được để trống")]
        [MinLength(6, ErrorMessage = "Password phải có ít nhất 6 ký tự")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }
    }
}
