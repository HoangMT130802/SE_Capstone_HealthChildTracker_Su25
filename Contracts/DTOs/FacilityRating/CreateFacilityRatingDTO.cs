using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.FacilityRating
{
    public class CreateFacilityRatingDTO
    {
        public int FacilityId { get; set; }
        public string Comment { get; set; }
        public int ServiceQuality { get; set; }
        public int FacilityCleanliness { get; set; }
        public int StaffAttitude { get; set; }
    }
}
