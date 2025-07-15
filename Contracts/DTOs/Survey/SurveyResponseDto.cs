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
    }
}
