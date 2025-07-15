using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.Survey
{
    public class CreateSurveyQuestionDto
    {
        public string QuestionText { get; set; }
        public string QuestionType { get; set; } 
        public bool IsRequired { get; set; }
    }
}
