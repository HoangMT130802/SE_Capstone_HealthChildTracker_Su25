using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.GrowthRecord
{
    public class CreateGrowthRecordDTO
    {
        [Required]
        [Range(30, 200, ErrorMessage = "Chiều cao phải từ 30cm đến 200cm")]
        public decimal Height { get; set; }
        
        [Required]
        [Range(2, 100, ErrorMessage = "Cân nặng phải từ 2kg đến 100kg")]
        public decimal Weight { get; set; }

        [Required]
        [Range(30, 100, ErrorMessage = "Chu vi đầu phải từ 30cm đến 100cm")]
        public decimal HeadCircumference { get; set; }
        [Required]
        public DateTime CreatedAt { get; set; }  
        public string Note { get; set; }
    }
}
