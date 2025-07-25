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
        Task<CostBreakdownDTO> CalculateEstimatedCostAsync(int facilityId, int? orderId = null, int? packageId = null, List<int>? facilityVaccineIds = null);
        
        // Booking Methods
        Task<AppointmentBookingResponseDTO> BookAppointmentAsync(AppointmentBookingRequestDTO request);
        Task<AppointmentQuickBookingResponseDTO> QuickBookAppointmentAsync(AppointmentQuickBookingDTO request);
        Task<bool> CancelAppointmentAsync(int appointmentId, string reason);
        
        // History Methods
        Task<AppointmentHistoryResponseDTO> GetAppointmentHistoryAsync(int memberId, int? childId = null);
        
        // Facility Staff Methods
        Task<FacilityAppointmentResponseDTO> GetAllFacilityAppointmentsAsync(int facilityId);
        Task<FacilityAppointmentResponseDTO> GetFacilityAppointmentsByDateAsync(int facilityId, DateTime date);
        Task<FacilityAppointmentResponseDTO> GetFacilityAppointmentsByWeekAsync(int facilityId, DateTime startOfWeek);
        Task<FacilityAppointmentResponseDTO> GetFacilityAppointmentsByMonthAsync(int facilityId, DateTime month);
        Task<FacilityAppointmentDTO> GetFacilityAppointmentByIdAsync(int appointmentId, int facilityId);
        Task<bool> UpdateAppointmentStatusAsync(int appointmentId, int facilityId, UpdateAppointmentStatusDTO updateDto);
        
        // Helper Methods
        Task<List<AppointmentSuggestionDTO>> GenerateAppointmentSuggestionsAsync(AppointmentQuickBookingDTO request, int maxSuggestions = 5);
    }
} 