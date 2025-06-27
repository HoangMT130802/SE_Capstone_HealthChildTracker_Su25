using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.Vaccine
{
    public class VaccineDTO
    {
        public int VaccineId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Manufacturer { get; set; }
        public string Category { get; set; }
        public string AgeGroup { get; set; }
        public int NumberOfDoses { get; set; }
        public int MinIntervalBetweenDoses { get; set; }
        public string SideEffects { get; set; }
        public string Contraindications { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<int> DiseaseIds { get; set; }
    }
}
