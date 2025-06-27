using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.Disease
{
    public class CreateDiseaseDTO
    {
        [Required(ErrorMessage = "Disease name is required")]
        [StringLength(100, ErrorMessage = "Disease name must be between 1 and 100 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(500, ErrorMessage = "Description must be between 1 and 500 characters")]
        public string Description { get; set; }

        [StringLength(500, ErrorMessage = "Symptoms must not exceed 500 characters")]
        public string Symptoms { get; set; }

        [StringLength(500, ErrorMessage = "Treatment must not exceed 500 characters")]
        public string Treatment { get; set; }
    }
}
