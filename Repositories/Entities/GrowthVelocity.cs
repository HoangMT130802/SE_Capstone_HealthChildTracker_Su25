using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Repositories.Entities
{
    [Table("GrowthVelocity")]
    public class GrowthVelocity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(10)]
        public string Gender { get; set; }

        [Required]
        public int AgeInMonths { get; set; }

        [Required]
        [StringLength(50)]
        public string Measurement { get; set; } // Height, Weight, HeadCircumference

        [Required]
        [Column(TypeName = "decimal(8,3)")]
        public decimal Sd3neg { get; set; } // Tốc độ tăng trưởng ở mức -3SD

        [Required]
        [Column(TypeName = "decimal(8,3)")]
        public decimal Sd2neg { get; set; } // Tốc độ tăng trưởng ở mức -2SD

        [Required]
        [Column(TypeName = "decimal(8,3)")]
        public decimal Sd1neg { get; set; } // Tốc độ tăng trưởng ở mức -1SD

        [Required]
        [Column(TypeName = "decimal(8,3)")]
        public decimal Median { get; set; } // Tốc độ tăng trưởng trung bình

        [Required]
        [Column(TypeName = "decimal(8,3)")]
        public decimal Sd1pos { get; set; } // Tốc độ tăng trưởng ở mức +1SD

        [Required]
        [Column(TypeName = "decimal(8,3)")]
        public decimal Sd2pos { get; set; } // Tốc độ tăng trưởng ở mức +2SD

        [Required]
        [Column(TypeName = "decimal(8,3)")]
        public decimal Sd3pos { get; set; } // Tốc độ tăng trưởng ở mức +3SD

        [StringLength(10)]
        public string Unit { get; set; } // cm/month, kg/month, cm/month

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}
