using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.ChildVaccineProfile
{
    public class CreateChildVaccineProfileDTO
    {
        [Required] 
        public int ChildId { get; set; }
        [Required]
        public int DiseaseId { get; set; }
        public int? AppointmentId { get; set; }
        [Required]
        public int VaccineId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Dose number must be greater than 0")]
        public int DoseNum { get; set; }

        [SwaggerSchema("The expected date of vaccination in format yyyy-MM-dd")]
        [Required]
        public DateOnly ExpectedDate { get; set; }

        [SwaggerSchema("The actual date of vaccination in format yyyy-MM-dd")]
        public DateOnly? ActualDate { get; set; }

        [Required]
        public string Status { get; set; }

        [Required]
        public bool IsRequired { get; set; }

        [Required]
        public string Priority { get; set; }
    }
}
