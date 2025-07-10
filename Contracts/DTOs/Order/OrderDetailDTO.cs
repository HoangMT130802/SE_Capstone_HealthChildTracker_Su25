using Contracts.DTOs.Disease;
using Contracts.DTOs.FacilityVaccine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.Order
{
    public class OrderDetailDTO
    {
        public int OrderDetailId { get; set; }
        public int OrderId { get; set; }
        public int FacilityVaccineId { get; set; }
        public FacilityVaccineDTO FacilityVaccine { get; set; }
        public int DiseaseId { get; set; }
        public DiseaseDTO Disease { get; set; }
        public int RemainingQuantity { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
