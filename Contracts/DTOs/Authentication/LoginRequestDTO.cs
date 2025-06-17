using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.Authentication
{
    public class LoginRequestDTO
    {
        [Required(ErrorMessage = "AccountName không được để trống")]
        public string AccountName { get; set; }

        [Required(ErrorMessage = "Password không được để trống")]
        public string Password { get; set; }
    }
}
