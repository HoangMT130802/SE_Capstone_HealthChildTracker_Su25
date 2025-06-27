using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.Vaccine
{
    public class CreateVaccineDTO
    {
        [Required(ErrorMessage = "Vaccine name is required")]
        [StringLength(100, ErrorMessage = "Vaccine name must be between 1 and 100 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(500, ErrorMessage = "Description must be between 1 and 500 characters")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Manufacturer is required")]
        [StringLength(100, ErrorMessage = "Manufacturer must be between 1 and 100 characters")]
        public string Manufacturer { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [StringLength(50, ErrorMessage = "Category must be between 1 and 50 characters")]
        public string Category { get; set; }

        [Required(ErrorMessage = "Age group is required")]
        [StringLength(50, ErrorMessage = "Age group must be between 1 and 50 characters")]
        public string AgeGroup { get; set; }

        [Required(ErrorMessage = "Number of doses is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Number of doses must be greater than 0")]
        public int NumberOfDoses { get; set; }

        [Required(ErrorMessage = "Minimum interval between doses is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Minimum interval must be non-negative")]
        public int MinIntervalBetweenDoses { get; set; }

        [StringLength(500, ErrorMessage = "Side effects must not exceed 500 characters")]
        public string SideEffects { get; set; }

        [StringLength(500, ErrorMessage = "Contraindications must not exceed 500 characters")]
        public string Contraindications { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be non-negative")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [StringLength(20, ErrorMessage = "Status must be between 1 and 20 characters")]
        public string Status { get; set; }

        public List<int> DiseaseIds { get; set; } = new List<int>();
    }
}
