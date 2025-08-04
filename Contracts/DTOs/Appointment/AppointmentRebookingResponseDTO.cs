using Contracts.DTOs.Child;
using Contracts.DTOs.ChildVaccineProfile;
using Contracts.DTOs.Disease;
using Contracts.DTOs.Appointment;
using Contracts.DTOs.Order;
using Contracts.DTOs.Vaccine;

namespace Contracts.DTOs.Appointment
{
    public class AppointmentRebookingResponseDTO
    {
        public int AppointmentId { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Note { get; set; }
        
        // Thông tin trẻ
        public ChildDTO Child { get; set; }
        
        // Thông tin bệnh và vaccine
        public DiseaseDTO Disease { get; set; }
        public VaccineDTO Vaccine { get; set; }
        public int DoseNumber { get; set; }
        
        // Thông tin lịch hẹn
        public AppointmentScheduleDTO Schedule { get; set; }
        
        // Thông tin thanh toán và gói
        public decimal EstimatedCost { get; set; }
        public bool UsedExistingOrder { get; set; }
        public OrderDTO? UsedOrder { get; set; }
        public int? RemainingVaccinesInOrder { get; set; }
        
        public string Message { get; set; }
    }
    
    public class AppointmentRebookingValidationDTO
    {
        public bool CanRebook { get; set; }
        public string? ReasonCannotRebook { get; set; }
        
        // Thông tin gói có thể sử dụng
        public bool HasApplicableOrder { get; set; }
        public OrderDTO? ApplicableOrder { get; set; }
        public int? AvailableVaccineQuantity { get; set; }
        
        // Thông tin chi phí
        public decimal EstimatedCost { get; set; }
        public bool RequiresPayment { get; set; }
        
        // Thông tin vaccine profile
        public ChildVaccineProfileDTO VaccineProfile { get; set; }
        public VaccineDTO Vaccine { get; set; }
        public DiseaseDTO Disease { get; set; }
    }
}