using AutoMapper;
using Contracts.DTOs.FacilitySchedule;
using Microsoft.Extensions.Logging;
using Repositories.Entities;
using Repositories.Interfaces;
using Services.Interfaces;

namespace Services.Implementations
{
    public class ScheduleSlotService : IScheduleSlotService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<ScheduleSlotService> _logger;

        public ScheduleSlotService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<ScheduleSlotService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ScheduleSlotDTO> CreateSlotAsync(CreateScheduleSlotDTO createDto)
        {
            try
            {
                _logger.LogInformation("Creating schedule slot");

                var slot = _mapper.Map<ScheduleSlot>(createDto);
                var repository = _unitOfWork.GetRepository<ScheduleSlot>();
                await repository.AddAsync(slot);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully created schedule slot");
                return _mapper.Map<ScheduleSlotDTO>(slot);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating schedule slot");
                throw;
            }
        }

        public async Task<ScheduleSlotDTO> GetSlotByIdAsync(int slotId)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<ScheduleSlot>();
                var slot = await repository.GetAsync(s => s.SlotId == slotId);

                if (slot == null)
                {
                    throw new ArgumentException($"Slot với ID {slotId} không tồn tại");
                }

                return _mapper.Map<ScheduleSlotDTO>(slot);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting slot by ID");
                throw;
            }
        }

        public async Task<List<ScheduleSlotDTO>> GetSlotsAsync(int page, int size, string? status = null)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<ScheduleSlot>();
                var slots = await repository.GetAllAsync("");
                
                var result = slots.Where(s => string.IsNullOrEmpty(status) || s.Status == status)
                                 .OrderBy(s => s.SlotTime)
                                 .ToList();

                return _mapper.Map<List<ScheduleSlotDTO>>(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting slots");
                throw;
            }
        }

        public async Task<ScheduleSlotDTO> UpdateSlotAsync(int slotId, UpdateScheduleSlotDTO updateDto)
        {
            try
            {
                _logger.LogInformation("Updating schedule slot");

                var repository = _unitOfWork.GetRepository<ScheduleSlot>();
                var slot = await repository.GetAsync(s => s.SlotId == slotId);

                if (slot == null)
                {
                    throw new ArgumentException($"Slot với ID {slotId} không tồn tại");
                }

                _mapper.Map(updateDto, slot);
                repository.Update(slot);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully updated schedule slot");
                return _mapper.Map<ScheduleSlotDTO>(slot);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating schedule slot");
                throw;
            }
        }

        public async Task<bool> DeleteSlotAsync(int slotId)
        {
            try
            {
                _logger.LogInformation("Deleting schedule slot");

                var repository = _unitOfWork.GetRepository<ScheduleSlot>();
                var slot = await repository.GetAsync(s => s.SlotId == slotId);

                if (slot == null)
                {
                    throw new ArgumentException($"Slot với ID {slotId} không tồn tại");
                }

                repository.Delete(slot);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted schedule slot");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting schedule slot");
                throw;
            }
        }

        public async Task<List<ScheduleSlotDTO>> GetActiveSlotsAsync()
        {
            try
            {
                var repository = _unitOfWork.GetRepository<ScheduleSlot>();
                var slots = await repository.GetAllAsync("");
                
                var activeSlots = slots.Where(s => s.Status == "Active")
                                      .OrderBy(s => s.SlotTime)
                                      .ToList();

                return _mapper.Map<List<ScheduleSlotDTO>>(activeSlots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active slots");
                throw;
            }
        }

        public async Task<List<ScheduleSlotDTO>> GetAvailableSlotsAsync()
        {
            try
            {
                var repository = _unitOfWork.GetRepository<ScheduleSlot>();
                var slots = await repository.GetAllAsync("");
                
                var availableSlots = slots.Where(s => s.Status == "Active" && s.BookedCount < s.MaxCapacity)
                                          .OrderBy(s => s.SlotTime)
                                          .ToList();

                return _mapper.Map<List<ScheduleSlotDTO>>(availableSlots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available slots");
                throw;
            }
        }

        public async Task<bool> IsSlotAvailableAsync(int slotId)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<ScheduleSlot>();
                var slot = await repository.GetAsync(s => s.SlotId == slotId);

                if (slot == null || slot.Status != "Active")
                {
                    return false;
                }

                return slot.BookedCount < slot.MaxCapacity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking slot availability");
                throw;
            }
        }

        public async Task<bool> UpdateBookedCountAsync(int slotId, int increment)
        {
            try
            {
                _logger.LogInformation("Updating booked count for slot");

                var repository = _unitOfWork.GetRepository<ScheduleSlot>();
                var slot = await repository.GetAsync(s => s.SlotId == slotId);

                if (slot == null)
                {
                    throw new ArgumentException($"Slot với ID {slotId} không tồn tại");
                }

                var newBookedCount = slot.BookedCount + increment;

                if (newBookedCount < 0)
                {
                    throw new InvalidOperationException("Số lượng đã đặt không thể nhỏ hơn 0");
                }

                if (newBookedCount > slot.MaxCapacity)
                {
                    throw new InvalidOperationException("Số lượng đã đặt vượt quá sức chứa");
                }

                slot.BookedCount = newBookedCount;
                slot.UpdatedAt = DateTime.UtcNow;

                repository.Update(slot);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully updated booked count");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating booked count");
                throw;
            }
        }

        public async Task<bool> ActivateSlotAsync(int slotId)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<ScheduleSlot>();
                var slot = await repository.GetAsync(s => s.SlotId == slotId);

                if (slot == null)
                {
                    throw new ArgumentException($"Slot với ID {slotId} không tồn tại");
                }

                slot.Status = "Active";
                slot.UpdatedAt = DateTime.UtcNow;

                repository.Update(slot);
                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating slot");
                throw;
            }
        }

        public async Task<bool> DeactivateSlotAsync(int slotId)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<ScheduleSlot>();
                var slot = await repository.GetAsync(s => s.SlotId == slotId);

                if (slot == null)
                {
                    throw new ArgumentException($"Slot với ID {slotId} không tồn tại");
                }

                slot.Status = "Inactive";
                slot.UpdatedAt = DateTime.UtcNow;

                repository.Update(slot);
                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating slot");
                throw;
            }
        }

        public async Task<bool> UpdateSlotCapacityAsync(int slotId, int newCapacity)
        {
            try
            {
                _logger.LogInformation("Updating slot capacity");

                var repository = _unitOfWork.GetRepository<ScheduleSlot>();
                var slot = await repository.GetAsync(s => s.SlotId == slotId);

                if (slot == null)
                {
                    throw new ArgumentException($"Slot với ID {slotId} không tồn tại");
                }

                if (newCapacity < 1)
                {
                    throw new ArgumentException("Sức chứa phải lớn hơn 0");
                }

                if (newCapacity < slot.BookedCount)
                {
                    throw new InvalidOperationException("Sức chứa mới không thể nhỏ hơn số lượng đã đặt");
                }

                slot.MaxCapacity = newCapacity;
                slot.UpdatedAt = DateTime.UtcNow;

                repository.Update(slot);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully updated slot capacity");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating slot capacity");
                throw;
            }
        }

        public async Task<List<ScheduleSlotDTO>> CreateDefaultSlotsAsync()
        {
            try
            {
                _logger.LogInformation("Creating default schedule slots");

                var defaultSlots = new List<CreateScheduleSlotDTO>
                {
                    new CreateScheduleSlotDTO { SlotTime = "08:00 - 09:00", MaxCapacity = 12, Status = "Active" },
                    new CreateScheduleSlotDTO { SlotTime = "09:00 - 10:00", MaxCapacity = 12, Status = "Active" },
                    new CreateScheduleSlotDTO { SlotTime = "10:00 - 11:00", MaxCapacity = 12, Status = "Active" },
                    new CreateScheduleSlotDTO { SlotTime = "11:00 - 11:30", MaxCapacity = 8, Status = "Active" },
                    new CreateScheduleSlotDTO { SlotTime = "13:00 - 14:00", MaxCapacity = 12, Status = "Active" },
                    new CreateScheduleSlotDTO { SlotTime = "14:00 - 15:00", MaxCapacity = 12, Status = "Active" },
                    new CreateScheduleSlotDTO { SlotTime = "15:00 - 16:00", MaxCapacity = 12, Status = "Active" },
                    new CreateScheduleSlotDTO { SlotTime = "16:00 - 17:00", MaxCapacity = 12, Status = "Active" }
                };

                var repository = _unitOfWork.GetRepository<ScheduleSlot>();
                var createdSlots = new List<ScheduleSlot>();

                foreach (var slotDto in defaultSlots)
                {
                    var slot = _mapper.Map<ScheduleSlot>(slotDto);
                    createdSlots.Add(slot);
                }

                await repository.AddRangeAsync(createdSlots);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully created default slots");
                return _mapper.Map<List<ScheduleSlotDTO>>(createdSlots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating default slots");
                throw;
            }
        }

        public async Task<bool> ValidateSlotTimeAsync(string slotTime)
        {
            try
            {
                if (string.IsNullOrEmpty(slotTime))
                {
                    return false;
                }

                var parts = slotTime.Split('-');
                if (parts.Length != 2)
                {
                    return false;
                }

                var startTimeStr = parts[0].Trim();
                var endTimeStr = parts[1].Trim();

                if (TimeSpan.TryParse(startTimeStr, out var startTime) &&
                    TimeSpan.TryParse(endTimeStr, out var endTime))
                {
                    return endTime > startTime;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CheckSlotTimeConflictAsync(string slotTime, int? excludeSlotId = null)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<ScheduleSlot>();
                var slots = await repository.GetAllAsync("");

                var existingSlots = slots.Where(s => !excludeSlotId.HasValue || s.SlotId != excludeSlotId.Value);

                foreach (var slot in existingSlots)
                {
                    if (slot.SlotTime == slotTime)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking slot time conflict");
                return true;
            }
        }

        public async Task<List<ScheduleSlotDTO>> CreateMultipleSlotsAsync(List<CreateScheduleSlotDTO> createDtos)
        {
            try
            {
                _logger.LogInformation("Creating multiple schedule slots");

                var repository = _unitOfWork.GetRepository<ScheduleSlot>();
                var createdSlots = new List<ScheduleSlot>();

                foreach (var createDto in createDtos)
                {
                    var slot = _mapper.Map<ScheduleSlot>(createDto);
                    createdSlots.Add(slot);
                }

                await repository.AddRangeAsync(createdSlots);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully created multiple schedule slots");
                return _mapper.Map<List<ScheduleSlotDTO>>(createdSlots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating multiple schedule slots");
                throw;
            }
        }

        public async Task<bool> UpdateMultipleSlotsStatusAsync(List<int> slotIds, string status)
        {
            try
            {
                _logger.LogInformation("Updating multiple slots status");

                var repository = _unitOfWork.GetRepository<ScheduleSlot>();
                var allSlots = await repository.GetAllAsync("");
                var slotsToUpdate = allSlots.Where(s => slotIds.Contains(s.SlotId)).ToList();

                foreach (var slot in slotsToUpdate)
                {
                    slot.Status = status;
                    slot.UpdatedAt = DateTime.UtcNow;
                }

                if (slotsToUpdate.Any())
                {
                    repository.UpdateRange(slotsToUpdate);
                    await _unitOfWork.SaveChangesAsync();
                }

                _logger.LogInformation("Successfully updated multiple slots status");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating multiple slots status");
                throw;
            }
        }
    }
} 