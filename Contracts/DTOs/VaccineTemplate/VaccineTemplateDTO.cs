using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.VaccineTemplate
{
    public class VaccineTemplateDTO
    {
        public int Id { get; set; }
        public int DiseaseId { get; set; }
        public string PeriodFrom { get; set; }
        public string PeriodTo { get; set; }
        public int DoseNum { get; set; }
        public string Description { get; set; }
        public bool IsRequired { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string DiseaseName { get; set; } 
    }
}
