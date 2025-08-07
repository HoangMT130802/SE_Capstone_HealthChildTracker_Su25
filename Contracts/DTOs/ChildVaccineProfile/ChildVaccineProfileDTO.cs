using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace Contracts.DTOs.ChildVaccineProfile
{
    public class ChildVaccineProfileDTO
    {
        public int VaccineProfileId { get; set; }
        public int ChildId { get; set; }
        public int DiseaseId { get; set; }
        public int? AppointmentId { get; set; }
        public int? FacilityId { get; set; } // Facility ID từ appointment nếu có
        public int VaccineId { get; set; }
        public int DoseNum { get; set; }
        public DateOnly ExpectedDate { get; set; }
        public DateOnly? ActualDate { get; set; }
        public string Status { get; set; }
        public bool IsRequired { get; set; }
        public string Priority { get; set; }
        public string Note { get; set; }
        
        [JsonIgnore]
        public DateTime CreatedAt { get; set; }
        
        [JsonIgnore]
        public DateTime UpdatedAt { get; set; }
    }
}
