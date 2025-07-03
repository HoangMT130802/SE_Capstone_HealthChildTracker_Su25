using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.VaccinePackage
{
    public class VaccinePackageDTO
    {
        public int PackageId { get; set; }
        public int FacilityId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Duration { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<PackageVaccineDTO> PackageVaccines { get; set; }
    }
}
