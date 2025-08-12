using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.FacilityStaff
{
    public class UpdateFacilityStaffDTO
    {
        public int? AccountId { get; set; }
        public int? FacilityId { get; set; }
        public string FullName { get; set; }
        public int? Phone { get; set; }
        public string Email { get; set; }
        public string Position { get; set; }
        public string Description { get; set; }
        public bool? Status { get; set; }
    }
}
