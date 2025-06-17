using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.GrowthStandard
{
    public class GrowthStandardDTO
    {
        public int Id { get; set; }
        public string Gender { get; set; }
        public int AgeInMonths { get; set; }
        public string Measurement { get; set; }
        public decimal Sd3neg { get; set; }
        public decimal Sd2neg { get; set; }
        public decimal Sd1neg { get; set; }
        public decimal Median { get; set; }
        public decimal Sd1pos { get; set; }
        public decimal Sd2pos { get; set; }
        public decimal Sd3pos { get; set; }
    }
}
