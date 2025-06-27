using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.ChildVaccineProfile
{
    public class ChildVaccineProfileDTO
    {
        public int VaccineProfileId { get; set; }
        public int ChildId { get; set; }
        public int? AppointmentId { get; set; }
        public int VaccineId { get; set; }
        public int DoseNum { get; set; }
        public DateOnly ExpectedDate { get; set; }
        public DateOnly? ActualDate { get; set; }
        public string Status { get; set; }
        public bool IsRequired { get; set; }
        public string Priority { get; set; }
        public long CreatedAt { get; set; }
        public long UpdatedAt { get; set; }
    }
}
