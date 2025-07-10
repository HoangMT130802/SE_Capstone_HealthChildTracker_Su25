using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.Order
{
    public class CreatePackageOrderDTO
    {
        public int PackageId { get; set; }
        public List<SelectedVaccineDTO> SelectedVaccines { get; set; } = new List<SelectedVaccineDTO>();
        public DateTime OrderDate { get; set; }
        public string Status { get; set; }
    }
}
