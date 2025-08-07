using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.FacilityRating
{
    public class FacilityRatingDTO
    {
        public int RatingId { get; set; }
        public int FacilityId { get; set; }
        public int MemberId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public int ServiceQuality { get; set; }
        public int FacilityCleanliness { get; set; }
        public int StaffAttitude { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
