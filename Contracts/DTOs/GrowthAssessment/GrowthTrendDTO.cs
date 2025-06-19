using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.GrowthAssessment
{
    public class GrowthTrendDTO
    {
        public bool HasSufficientData { get; set; }
        public decimal HeightVelocity { get; set; }
        public decimal WeightVelocity { get; set; }
        public decimal BMIVelocity { get; set; }
        public DateTime LastMeasurementDate { get; set; }
        public int NumberOfMeasurements { get; set; }
        public bool IsGrowthConcerning { get; set; }
    }
}
