using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOsDTOs.Authentication
{
    public class ResetPasswordDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string ResetToken { get; set; }

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; }

        [Required]
        [Compare("NewPassword")]
        public string ConfirmPassword { get; set; }
    }
}
