using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.Dashboard
{
    public class FacilityDashboardDTO
    {
        public int FacilityId { get; set; }
        public int TotalFacilityVaccines { get; set; }
        public int TotalOrders { get; set; }
        public int TotalPackageVaccines { get; set; }
        public double AverageRating { get; set; }
        public RevenueStatsDTO RevenueStats { get; set; }
        public StaffCountsDTO StaffCounts { get; set; }
        public AppointmentStatsDTO AppointmentStats { get; set; }
    }
}
