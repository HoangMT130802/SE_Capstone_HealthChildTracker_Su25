using System;
using System.Collections.Generic;

namespace Contracts.DTOs.Survey
{
    public class AppointmentSurveyResponseDto
    {
        public int AppointmentId { get; set; }
        public DateTime SubmittedAt { get; set; }

        // Vital signs - chỉ hiển thị một lần cho mỗi appointment
        public decimal? TemperatureC { get; set; }
        public int? HeartRateBpm { get; set; }
        public int? SystolicBpmmHg { get; set; }
        public int? DiastolicBpmmHg { get; set; }
        public int? OxygenSatPercent { get; set; }
        public string DecisionNote { get; set; }
        public bool? ConsentObtained { get; set; }

        // Danh sách câu hỏi và câu trả lời
        public List<SurveyQuestionAnswerDto> Questions { get; set; } = new List<SurveyQuestionAnswerDto>();
    }

    public class SurveyQuestionAnswerDto
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; }
        public string AnswerText { get; set; }
    }
}
