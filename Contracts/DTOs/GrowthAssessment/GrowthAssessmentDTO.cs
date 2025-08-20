using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.GrowthAssessment
{
    public class GrowthAssessmentDTO
    {
        public int RecordId { get; set; }
        public int ChildId { get; set; }
        public DateTime MeasurementDate { get; set; }
        public decimal Height { get; set; }
        public decimal Weight { get; set; }
        public decimal BMI { get; set; }
        public decimal HeadCircumference { get; set; }
        public GrowthAssessmentsDTO Assessments { get; set; }
        public string Recommendations { get; set; }
        
        // Disclaimer cho API cơ bản
        public string MedicalDisclaimer { get; set; } = "📝 **DISCLAIMER**: Thông tin này chỉ mang tính tham khảo. Luôn tham vấn bác sĩ nhi khoa trước khi đưa ra quyết định về sức khỏe của trẻ.";
    }
}
