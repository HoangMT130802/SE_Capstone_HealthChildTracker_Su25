using Contracts.DTOs.Vaccine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.FacilityVaccine
{
    public class FacilityVaccineDTO
    {
        public int FacilityVaccineId { get; set; }
        public int FacilityId { get; set; }
        public int VaccineId { get; set; }
        public decimal Price { get; set; }
        public int AvailableQuantity { get; set; }
        public int BatchNumber { get; set; }
        public DateOnly ExpiryDate { get; set; }
        public DateOnly ImportDate { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public VaccineDTO Vaccine { get; set; }
    }
}
