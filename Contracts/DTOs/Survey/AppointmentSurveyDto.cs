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
    }
}
