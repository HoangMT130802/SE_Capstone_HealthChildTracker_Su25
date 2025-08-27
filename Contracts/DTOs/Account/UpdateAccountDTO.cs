using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.Account
{
    public class UpdateAccountDTO
    {
        public IFormFile? Image { get; set; }
    }
}
