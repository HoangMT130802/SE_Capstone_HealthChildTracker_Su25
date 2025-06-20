using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.Child
{
    public class CreateChildDTO
    {
        [Required(ErrorMessage = "Tên đầy đủ là bắt buộc")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Tên phải từ 2-100 ký tự")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Ngày sinh là bắt buộc")]
        [DataType(DataType.Date)]
        public DateTime BirthDate { get; set; }

        [Required(ErrorMessage = "Giới tính là bắt buộc")]
        [RegularExpression("^(Male|Female|MALE|FEMALE|male|female)$", 
            ErrorMessage = "Giới tính chỉ được nhận giá trị: Male, Female")]
        public string Gender { get; set; }

        [RegularExpression("^(A|B|AB|O)[+-]?$", 
            ErrorMessage = "Nhóm máu phải theo định dạng: A, B, AB, O (có thể có + hoặc -)")]
        public string? BloodType { get; set; }

        public string? AllergiesNotes { get; set; }

        public string? MedicalHistory { get; set; }
    }
}
