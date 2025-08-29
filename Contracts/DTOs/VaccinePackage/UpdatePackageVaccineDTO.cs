using Contracts.DTOs.Order;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.VaccinePackage
{
    public class UpdatePackageVaccineDTO
    {
        [Required(ErrorMessage = "Vaccine updates are required")]
        public List<SelectedVaccineDTO> SelectedVaccines { get; set; }
    }
}
