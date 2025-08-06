using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.ChildVaccineProfile
{
    public class VaccineRecordDTO
    {
        public int DiseaseId { get; set; }
        public string DiseaseName { get; set; }
        public int RequiredDoseNum { get; set; }
        public int CompletedDoseNum { get; set; }
        public bool IsRequired { get; set; }
        public string Status { get; set; } // "Đã đủ liều", "Chưa đủ liều", "Chưa tiêm"
        public string PeriodFrom { get; set; }
        public string PeriodTo { get; set; }
    }
}
