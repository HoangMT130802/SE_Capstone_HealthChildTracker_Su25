using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.GrowthRecord
{
    public class GrowthRecordDTO
    {
        public int RecordId { get; set; }
        public int ChildId { get; set; }
        public string ChildName { get; set; }
        public decimal Height { get; set; }
        public decimal Weight { get; set; }
        public decimal Bmi { get; set; }  
        public decimal HeadCircumference { get; set; }
        public int AgeInDays { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }  
        public DateTime UpdatedAt { get; set; }  
    }
}
