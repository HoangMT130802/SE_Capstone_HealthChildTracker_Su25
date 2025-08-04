using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.VaccineTemplate
{
    public class UpdateVaccineTemplateDTO
    {
        [Required(ErrorMessage = "Disease ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Disease ID must be greater than 0")]
        public int DiseaseId { get; set; }

        [Required(ErrorMessage = "PeriodFrom is required")]
        [StringLength(50, ErrorMessage = "PeriodFrom must be between 1 and 50 characters")]
        public string PeriodFrom { get; set; }

        [Required(ErrorMessage = "PeriodTo is required")]
        [StringLength(50, ErrorMessage = "PeriodTo must be between 1 and 50 characters")]
        public string PeriodTo { get; set; }

        [Required(ErrorMessage = "Dose number is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Dose number must be greater than 0")]
        public int DoseNum { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(500, ErrorMessage = "Description must be between 1 and 500 characters")]
        public string Description { get; set; }

        [Required(ErrorMessage = "IsRequired is required")]
        public bool IsRequired { get; set; }
    }
}
