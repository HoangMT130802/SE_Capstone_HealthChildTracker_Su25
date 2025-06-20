using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.DailyRecord
{
    public class DailyRecordDTO
    {
        public int DailyRecordId { get; set; }

        public int ChildId { get; set; }

        public DateOnly RecordDate { get; set; }

        public int MilkAmount { get; set; }

        public int FeedingTimes { get; set; }

        public int DiaperChanges { get; set; }

        public decimal SleepHours { get; set; }

        public string Note { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
