using System;

namespace Contracts.DTOs.GrowthStandard
{
    public class GrowthVelocityAssessmentDTO
    {
        public string Gender { get; set; }
        public int AgeInMonths { get; set; }
        public string Measurement { get; set; }
        public decimal ActualVelocity { get; set; }
        public string Unit { get; set; }
        
        // ✅ Đánh giá tốc độ tăng trưởng
        public string VelocityStatus { get; set; }
        public string VelocityDescription { get; set; }
        public decimal ExpectedVelocity { get; set; }
        public decimal VelocityPercentile { get; set; }
        
        // ✅ So sánh với chuẩn WHO
        public decimal Sd3neg { get; set; }
        public decimal Sd2neg { get; set; }
        public decimal Sd1neg { get; set; }
        public decimal Median { get; set; }
        public decimal Sd1pos { get; set; }
        public decimal Sd2pos { get; set; }
        public decimal Sd3pos { get; set; }
        
        // ✅ Khuyến nghị
        public string Recommendation { get; set; }
        public bool RequiresMedicalAttention { get; set; }
        public string MedicalAdvice { get; set; }
        
        // ✅ Thông tin bổ sung
        public DateTime AssessmentDate { get; set; } = DateTime.Now;
        public string Notes { get; set; }
    }
}
