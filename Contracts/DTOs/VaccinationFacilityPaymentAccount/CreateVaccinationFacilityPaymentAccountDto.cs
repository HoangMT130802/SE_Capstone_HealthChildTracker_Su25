using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.VaccinationFacilityPaymentAccount
{
    public class CreateVaccinationFacilityPaymentAccountDto
    {
        [Required(ErrorMessage = "Facility ID is required")]
        public int FacilityId { get; set; }

        [Required(ErrorMessage = "Bank name is required")]
        [StringLength(100, ErrorMessage = "Bank name cannot exceed 100 characters")]
        public string BankName { get; set; }

        [Required(ErrorMessage = "Account number is required")]
        [StringLength(50, ErrorMessage = "Account number cannot exceed 50 characters")]
        public string AccountNumber { get; set; }

        [Required(ErrorMessage = "Account holder is required")]
        [StringLength(100, ErrorMessage = "Account holder name cannot exceed 100 characters")]
        public string AccountHolder { get; set; }

        // Thay URL bằng file
        [Required(ErrorMessage = "QR code image is required")]
        public IFormFile QrcodeImage { get; set; }

        [Required(ErrorMessage = "IsActive is required")]
        public bool IsActive { get; set; }
    }
}
