using System;
using System.Collections.Generic;

namespace Contracts.DTOs.GrowthAssessment
{
    public class GrowthPredictionDTO
    {
        public int ChildId { get; set; }
        public string ChildName { get; set; }
        public DateTime LastMeasurementDate { get; set; }
        public string PredictionMethod { get; set; }
        public List<PredictionDataPointDTO> PredictionPoints { get; set; } = new List<PredictionDataPointDTO>();
        public string Recommendations { get; set; }
        public int DataPointsUsed { get; set; } // Số điểm dữ liệu đã sử dụng để dự đoán
        
        // Thông tin về độ tin cậy và disclaimer
        public PredictionQualityDTO PredictionQuality { get; set; }
        public string MedicalDisclaimer { get; set; } = "📝 **DISCLAIMER BẮT BUỘC**: Thông tin này chỉ mang tính tham khảo. Luôn tham vấn bác sĩ nhi khoa trước khi đưa ra quyết định về sức khỏe của trẻ.";
        public bool RequiresMedicalConsultation { get; set; }
        public List<string> DataLimitations { get; set; } = new List<string>();
    }

    public class PredictionQualityDTO
    {
        public double OverallConfidence { get; set; } // 0-100%
        public string ConfidenceLevel { get; set; } // "Cao", "Trung bình", "Thấp"
        public int DataPointsUsed { get; set; }
        public double TrendConsistency { get; set; }
        public string DataQualityDescription { get; set; }
        public List<string> QualityWarnings { get; set; } = new List<string>();
    }

    public class PredictionDataPointDTO
    {
        public DateTime PredictedDate { get; set; }
        public int AgeInDays { get; set; }
        public decimal PredictedHeight { get; set; }
        public decimal PredictedWeight { get; set; }
        public decimal PredictedBMI { get; set; }
        public decimal PredictedHeadCircumference { get; set; }
        public string TimeLabel { get; set; } // "1 tuần", "1 tháng", etc.
    }
} 