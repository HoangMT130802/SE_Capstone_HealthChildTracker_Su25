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