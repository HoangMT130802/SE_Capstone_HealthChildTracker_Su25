using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.FacilityVaccine
{
    public class UpdateFacilityVaccineDTO
    {
        [Required(ErrorMessage = "Facility ID is required")]
        public int FacilityId { get; set; }

        [Required(ErrorMessage = "Vaccine ID is required")]
        public int VaccineId { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be non-negative")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Available quantity is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Available quantity must be non-negative")]
        public int AvailableQuantity { get; set; }

        [Required(ErrorMessage = "Batch number is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Batch number must be positive")]
        public int BatchNumber { get; set; }

        [Required(ErrorMessage = "Expiry date is required")]
        public DateOnly ExpiryDate { get; set; }

        [Required(ErrorMessage = "Import date is required")]
        public DateOnly ImportDate { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [StringLength(20, ErrorMessage = "Status must be between 1 and 20 characters")]
        public string Status { get; set; }
    }
}
