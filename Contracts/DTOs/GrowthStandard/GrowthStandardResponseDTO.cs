using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.GrowthStandard
{
    public class GrowthStandardResponseDTO
    {
        public List<GrowthStandardDTO> Standards { get; set; }
        public string Measurement { get; set; }
    }
}
