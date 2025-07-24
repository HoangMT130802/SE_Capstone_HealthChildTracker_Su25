namespace Contracts.DTOs.Authentication
{
    public class DoctorProfileDTO
    {
        public int DoctorId { get; set; }
        public int Age { get; set; }
        public string Specialization { get; set; }
        public string Certifications { get; set; }
        public string University { get; set; }
        public string Bio { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
} 