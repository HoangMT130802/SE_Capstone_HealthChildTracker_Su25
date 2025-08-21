using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.Survey
{
    public class SurveyResponseDto
    {
        public int AppointmentId { get; set; }
        public int QuestionId { get; set; }
        public string QuestionText { get; set; }
        public string AnswerText { get; set; }
        public DateTime SubmittedAt { get; set; }

        // Các trường thăm khám bác sĩ (chỉ hiển thị khi có dữ liệu)
        public decimal? TemperatureC { get; set; }
        public int? HeartRateBpm { get; set; }
        public int? SystolicBpmmHg { get; set; }
        public int? DiastolicBpmmHg { get; set; }
        public int? OxygenSatPercent { get; set; }
        public string DecisionNote { get; set; }
        public bool? ConsentObtained { get; set; }
    }
}
