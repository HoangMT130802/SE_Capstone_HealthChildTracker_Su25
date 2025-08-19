using Contracts.DTOs.Appointment;
using Contracts.DTOs.Dashboard;
using Contracts.DTOs.Models;

namespace Services.Interfaces
{
    public interface IAppointmentBookingService
    {
        // Search & Filter Methods
        Task<FacilitySearchByDiseaseDTO> SearchFacilitiesByDiseaseAsync(int diseaseId, AppointmentSearchFiltersDTO? filters = null);
        Task<FacilityVaccinesByDiseaseDTO> GetFacilityVaccinesByDiseaseAsync(int facilityId, int diseaseId);
        Task<AvailableSchedulesDTO> GetAvailableSchedulesAsync(int facilityId, DateOnly fromDate, DateOnly toDate, List<string>? preferredTimeSlots = null);
        
        // Validation Methods
        Task<AppointmentValidationDTO> ValidateBookingRequestAsync(AppointmentBookingRequestDTO request);
        Task<ChildVaccinationHistoryDTO> GetChildVaccinationHistoryAsync(int childId, int diseaseId);
        
        // Cost Calculation Methods
        Task<CostBreakdownDTO> CalculateEstimatedCostAsync(int facilityId, int? orderId = null, int? packageId = null, List<int>? facilityVaccineIds = null);
        
        // Booking Methods
        Task<ResponseDataModel<AppointmentBookingResponseDTO>> BookAppointmentAsync(AppointmentBookingRequestDTO request);
        Task<ResponseDataModel<AppointmentQuickBookingResponseDTO>> QuickBookAppointmentAsync(AppointmentQuickBookingDTO request);
        Task<ResponseDataModel<bool>> CancelAppointmentAsync(int appointmentId, string reason);
        
        // Rebooking Methods
        Task<ResponseDataModel<AppointmentRebookingValidationDTO>> ValidateRebookingRequestAsync(int childVaccineProfileId, int accountId);
        Task<ResponseDataModel<AppointmentRebookingResponseDTO>> RebookAppointmentAsync(AppointmentRebookingRequestDTO request, int accountId);
        
        // History Methods
        Task<AppointmentHistoryResponseDTO> GetAppointmentHistoryAsync(int memberId, int? childId = null);
        
        // Facility Staff Methods  
        Task<FacilityAppointmentResponseDTO> GetAllFacilityAppointmentsAsync(int facilityId, int pageIndex = 1, int pageSize = 50);
        Task<FacilityAppointmentResponseDTO> GetFacilityAppointmentsByDateAsync(int facilityId, DateTime date, int pageIndex = 1, int pageSize = 50);
        Task<FacilityAppointmentResponseDTO> GetFacilityAppointmentsByWeekAsync(int facilityId, DateTime startOfWeek, int pageIndex = 1, int pageSize = 50);
        Task<FacilityAppointmentResponseDTO> GetFacilityAppointmentsByMonthAsync(int facilityId, DateTime month, int pageIndex = 1, int pageSize = 50);
        Task<FacilityAppointmentDTO> GetFacilityAppointmentByIdAsync(int appointmentId, int facilityId);
        Task<bool> UpdateAppointmentStatusAsync(int appointmentId, int facilityId, UpdateAppointmentStatusDTO updateDto);
        
        // Manager Methods  
        Task<bool> ApproveRefundAsync(int appointmentId, int facilityId, string? note = null);
        
        // Helper Methods
        Task<List<AppointmentSuggestionDTO>> GenerateAppointmentSuggestionsAsync(AppointmentQuickBookingDTO request, int maxSuggestions = 5);
        Task<AppointmentStatsDTO> GetAppointmentStatsByFacilityAsync(int facilityId);
        Task<AppointmentStatsDTO> GetAppointmentStatsAsync();
    }
} 