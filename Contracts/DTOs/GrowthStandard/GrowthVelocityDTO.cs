using System;

namespace Contracts.DTOs.GrowthStandard
{
    public class GrowthVelocityDTO
    {
        public int Id { get; set; }
        public string Gender { get; set; }
        public int AgeInMonths { get; set; }
        public int AgeInDays => AgeInMonths * 30; // Tính toán AgeInDays từ AgeInMonths
        public string Measurement { get; set; }
        public decimal Sd3neg { get; set; }
        public decimal Sd2neg { get; set; }
        public decimal Sd1neg { get; set; }
        public decimal Median { get; set; }
        public decimal Sd1pos { get; set; }
        public decimal Sd2pos { get; set; }
        public decimal Sd3pos { get; set; }
        public string Unit { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
