using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.VaccinePackage
{
    public class CreateVaccinePackageDTO
    {
        [Required(ErrorMessage = "Facility ID is required")]
        public int FacilityId { get; set; }

        [Required(ErrorMessage = "Package name is required")]
        [StringLength(100, ErrorMessage = "Package name must be between 1 and 100 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(500, ErrorMessage = "Description must be between 1 and 500 characters")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Duration is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Duration must be non-negative")]
        public int Duration { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be non-negative")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [StringLength(20, ErrorMessage = "Status must be between 1 and 20 characters")]
        public string Status { get; set; }
    }
}
