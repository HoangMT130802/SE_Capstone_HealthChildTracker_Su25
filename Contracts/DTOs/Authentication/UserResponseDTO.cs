using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.Authentication
{
    public class UserResponseDTO
    {
        public int AccountId { get; set; }
        public string AccountName { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; } // Member luôn có thông tin này
        public string Phone { get; set; } // Member luôn có thông tin này
        public string Address { get; set; } // Member luôn có thông tin này
        public string Role { get; set; }
        public bool Status { get; set; }
        public string Token { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Thêm fields cho FacilityStaff
        public int? StaffId { get; set; }
        public string? Position { get; set; }
        public int? FacilityId { get; set; }
    }
}
