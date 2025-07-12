using Contracts.DTOs.Appointment;

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
        Task<CostBreakdownDTO> CalculateEstimatedCostAsync(int facilityId, int? packageId = null, List<int>? facilityVaccineIds = null);
        
        // Booking Methods
        Task<AppointmentBookingResponseDTO> BookAppointmentAsync(AppointmentBookingRequestDTO request);
        Task<AppointmentQuickBookingResponseDTO> QuickBookAppointmentAsync(AppointmentQuickBookingDTO request);
        Task<bool> CancelAppointmentAsync(int appointmentId, string reason);
        
        // Helper Methods
        Task<List<AppointmentSuggestionDTO>> GenerateAppointmentSuggestionsAsync(AppointmentQuickBookingDTO request, int maxSuggestions = 5);
    }
} 