using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.Dashboard
{
    public class AdminDashboardDTO
    {
        public int TotalFacilities { get; set; }
        public decimal TotalRevenue { get; set; } 
        public int TotalChildren { get; set; }
        public int TotalMembershipPackages { get; set; } 
        public int TotalUserMemberships { get; set; } 
        public decimal TotalRevenueFromMemberships { get; set; } 
        public int TotalGrowthRecords { get; set; }
        public AppointmentStatsDTO AppointmentStats { get; set; }
    }
}
