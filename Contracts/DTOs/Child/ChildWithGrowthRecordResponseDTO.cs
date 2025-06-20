using Contracts.DTOs.GrowthRecord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.Child
{
    public class ChildWithGrowthRecordResponseDTO
    {
        public ChildDTO Child { get; set; }
        public GrowthRecordDTO GrowthRecord { get; set; }
        public string Message { get; set; }
    }
} 