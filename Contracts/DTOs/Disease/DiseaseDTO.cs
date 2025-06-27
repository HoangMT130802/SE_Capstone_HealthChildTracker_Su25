using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.Disease
{
    public class DiseaseDTO
    {
        public int DiseaseId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Symptoms { get; set; }
        public string Treatment { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
