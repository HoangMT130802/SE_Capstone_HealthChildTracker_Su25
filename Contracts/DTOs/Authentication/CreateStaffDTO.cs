using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.Authentication
{
    public class CreateStaffDTO
    {
        [Required]
        public string AccountName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string FullName { get; set; }

        public string? Phone { get; set; }

        [Required]
        public string Position { get; set; } // "Doctor" hoặc "Staff"

        public string? Description { get; set; }

        // ✅ Optional DoctorProfile fields (chỉ dùng khi Position = "Doctor")
        public int? Age { get; set; }
        public string? Specialization { get; set; }
        public string? Certifications { get; set; }
        public string? University { get; set; }
        public string? Bio { get; set; }
    }
} 