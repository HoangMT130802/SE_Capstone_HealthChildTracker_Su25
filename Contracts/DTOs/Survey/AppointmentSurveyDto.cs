using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.Survey
{
    public class AppointmentSurveyDto
    {
        [Required(ErrorMessage = "Question ID is required")]
        public int QuestionId { get; set; }

        public int? AnswerId { get; set; }

        [StringLength(1000, ErrorMessage = "Answer text cannot exceed 1000 characters")]
        public string AnswerText { get; set; }

        // Các trường thăm khám bác sĩ sẽ điền khi submit survey
        [Range(30, 45, ErrorMessage = "Temperature must be between 30°C and 45°C")]
        public decimal? TemperatureC { get; set; }

        [Range(40, 200, ErrorMessage = "Heart rate must be between 40 and 200 BPM")]
        public int? HeartRateBpm { get; set; }

        [Range(70, 250, ErrorMessage = "Systolic blood pressure must be between 70 and 250 mmHg")]
        public int? SystolicBpmmHg { get; set; }

        [Range(40, 150, ErrorMessage = "Diastolic blood pressure must be between 40 and 150 mmHg")]
        public int? DiastolicBpmmHg { get; set; }

        [Range(70, 100, ErrorMessage = "Oxygen saturation must be between 70% and 100%")]
        public int? OxygenSatPercent { get; set; }

        [StringLength(2000, ErrorMessage = "Decision note cannot exceed 2000 characters")]
        public string DecisionNote { get; set; }

        public bool? ConsentObtained { get; set; }
    }
}
