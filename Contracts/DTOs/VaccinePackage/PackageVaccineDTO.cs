using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.VaccinePackage
{
    public class PackageVaccineDTO
    {
        public int PackageVaccineId { get; set; }
        public int PackageId { get; set; }
        public int VaccineId { get; set; }
        public int Quantity { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
