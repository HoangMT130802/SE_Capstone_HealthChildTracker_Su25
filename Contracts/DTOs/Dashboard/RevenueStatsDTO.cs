using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.Dashboard
{
    public class RevenueStatsDTO
    {
        public decimal PaidRevenue { get; set; } 
        public int PendingOrdersCount { get; set; } 
    }
}
