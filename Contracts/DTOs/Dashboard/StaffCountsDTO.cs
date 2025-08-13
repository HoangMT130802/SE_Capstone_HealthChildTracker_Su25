using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.Dashboard
{
    public class StaffCountsDTO
    {
        public int TotalStaffs { get; set; }
        public int TotalManagers { get; set; }
        public int TotalDoctors { get; set; }
    }
}
