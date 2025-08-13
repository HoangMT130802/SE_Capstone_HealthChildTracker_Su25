using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.Dashboard
{
    public class AppointmentStatsDTO
    {
        public int TotalAppointments { get; set; } 
        public int PackageAppointments { get; set; } 
        public int IndividualAppointments { get; set; } 
        public int Pending { get; set; }
        public int Completed { get; set; }
        public int Approval { get; set; }
        public int Cancelled { get; set; }
        public int Paid { get; set; }
        public int UniqueChildrenVaccinated { get; set; } 
    }
}
