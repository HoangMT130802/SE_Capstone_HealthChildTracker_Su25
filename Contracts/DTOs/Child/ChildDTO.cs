using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.Child
{
    public class ChildDTO
    {
        public int ChildId { get; set; }
        public int MemberId { get; set; }
        public string FullName { get; set; }
        public DateTime BirthDate { get; set; }
        public string Gender { get; set; }
        public string BloodType { get; set; }
        public string AllergiesNotes { get; set; }
        public string MedicalHistory { get; set; }
        public string imageURL { get; set; }
        public bool Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdateAt { get; set; }
    }
}
