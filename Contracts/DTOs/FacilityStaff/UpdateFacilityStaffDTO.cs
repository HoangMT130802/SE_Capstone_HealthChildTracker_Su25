using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.FacilityStaff
{
    public class UpdateFacilityStaffDTO
    {
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Position { get; set; }
        public string Description { get; set; }
        public bool? Status { get; set; }
    }
}
