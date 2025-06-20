using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.DailyRecord
{
    public class CreateDailyRecordDTO
    {
        public int ChildId { get; set; }
        [SwaggerSchema("The date of the record in format yyyy-MM-dd")]
        [Required]
        public DateOnly RecordDate { get; set; }

        [Required]
        [Range(0, 5000, ErrorMessage = "Lượng sữa không được bé hơn 0 ml")]
        public int MilkAmount { get; set; }

        [Required]
        [Range(0, 20, ErrorMessage = "Số lần cho ăn không được bé hơn 0")]
        public int FeedingTimes { get; set; }

        [Required]
        [Range(0, 100, ErrorMessage = "Số lần thay tả không được bé hơn 0")]
        public int DiaperChanges { get; set; }

        [Required]
        [Range(0, 24, ErrorMessage = "Lượng Giờ ngủ trong ngày phải từ 0 giờ đến 24 giờ")]
        public decimal SleepHours { get; set; }


        public string Note { get; set; }
    }
}
