using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.Order
{
    public class SelectedVaccineDTO
    {
        public int DiseaseId { get; set; }
        public int FacilityVaccineId { get; set; }
        public int Quantity { get; set; }
    }
}
