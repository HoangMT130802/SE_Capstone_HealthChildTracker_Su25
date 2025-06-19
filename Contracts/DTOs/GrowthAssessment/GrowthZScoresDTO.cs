using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.GrowthAssessment
{
    public class GrowthZScoresDTO
    {
        public decimal Height { get; set; }
        public decimal Weight { get; set; }
        public decimal BMI { get; set; }
        public decimal HeadCircumference { get; set; }
    }
}
