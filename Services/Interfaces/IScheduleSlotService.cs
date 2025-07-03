using Contracts.DTOs.FacilitySchedule;
using Repositories.Models.QueryModels;

namespace Services.Interfaces
{
    public interface IScheduleSlotService
    {
        // CRUD Operations
        Task<ScheduleSlotDTO> CreateSlotAsync(CreateScheduleSlotDTO createDto);
        Task<ScheduleSlotDTO> GetSlotByIdAsync(int slotId);
        Task<List<ScheduleSlotDTO>> GetSlotsAsync(int page, int size, string? status = null);
        Task<ScheduleSlotDTO> UpdateSlotAsync(int slotId, UpdateScheduleSlotDTO updateDto);
        Task<bool> DeleteSlotAsync(int slotId);

        // Business Operations
        Task<List<ScheduleSlotDTO>> GetActiveSlotsAsync();
        Task<List<ScheduleSlotDTO>> GetAvailableSlotsAsync();
        Task<bool> IsSlotAvailableAsync(int slotId);
        Task<bool> UpdateBookedCountAsync(int slotId, int increment);

        // Slot Management
        Task<bool> ActivateSlotAsync(int slotId);
        Task<bool> DeactivateSlotAsync(int slotId);
        Task<bool> UpdateSlotCapacityAsync(int slotId, int newCapacity);

        // Default Slot Operations
        Task<List<ScheduleSlotDTO>> CreateDefaultSlotsAsync();
        Task<bool> ValidateSlotTimeAsync(string slotTime);
        Task<bool> CheckSlotTimeConflictAsync(string slotTime, int? excludeSlotId = null);

        // Batch Operations
        Task<List<ScheduleSlotDTO>> CreateMultipleSlotsAsync(List<CreateScheduleSlotDTO> createDtos);
        Task<bool> UpdateMultipleSlotsStatusAsync(List<int> slotIds, string status);
    }
} 