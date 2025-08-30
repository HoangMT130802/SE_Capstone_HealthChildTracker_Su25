using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.GrowthRecord
{
    public class UpdateGrowthRecordDTO
    {
       
        [Required]
        [Range(30, 200, ErrorMessage = "Chiều cao phải từ 20cm đến 200cm")]
        public decimal Height { get; set; }

        [Required]
        [Range(2, 100, ErrorMessage = "Cân nặng phải từ 0,5kg đến 150kg")]
        public decimal Weight { get; set; }

        [Required]
        [Range(30, 100, ErrorMessage = "Chu vi đầu phải từ 20cm đến 80cm")]
        public decimal HeadCircumference { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; }
        
        public string Note { get; set; }
    }
}
