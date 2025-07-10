using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.Order
{
    public class UpdateOrderDTO
    {
        public DateTime OrderDate { get; set; }
        public string Status { get; set; }
    }
}
