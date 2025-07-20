using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.Survey
{
    public class SurveyQuestionDto
    {
        public int QuestionId { get; set; }

        [Required(ErrorMessage = "Question text is required")]
        [StringLength(1000, ErrorMessage = "Question text cannot exceed 1000 characters")]
        public int SurveyId { get; set; }
        public string QuestionText { get; set; }

        [Required(ErrorMessage = "Question type is required")]
        [RegularExpression("Text|MultipleChoice|YesNo", ErrorMessage = "Question type must be Text, MultipleChoice, or YesNo")]
        public string QuestionType { get; set; }

        public bool IsRequired { get; set; }
    }
}
