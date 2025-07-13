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

        public async Task<List<ScheduleSlotDTO>> GetAllSlotsAsync()
        {
            try
            {
                var repository = _unitOfWork.GetRepository<ScheduleSlot>();
                var slotsResult = await repository.GetAllAsync(null, null, "Facility", null, null);
                var slots = slotsResult.Data;
                
                var result = slots.OrderBy(s => s.SlotTime).ToList();
                var mappedSlots = _mapper.Map<List<ScheduleSlotDTO>>(result);
                
                // ✅ Tính BookedCount tự động cho tất cả slots
                await CalculateBookedCountForSlots(mappedSlots);
                
                // Gán SlotNumber theo thứ tự
                for (int i = 0; i < mappedSlots.Count; i++)
                {
                    mappedSlots[i].SlotNumber = i + 1;
                }
                
                return mappedSlots;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách slots");
                throw;
            }
        }

        // ✅ Thêm method GetSlotsByFacilityAsync cho phân quyền
        public async Task<List<ScheduleSlotDTO>> GetSlotsByFacilityAsync(int facilityId)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<ScheduleSlot>();
                var allSlotsResult = await repository.GetAllAsync(null, null, "Facility", null, null);
                var allSlots = allSlotsResult.Data;
                var slots = allSlots.Where(s => s.FacilityId == facilityId).OrderBy(s => s.SlotTime).ToList();
                
                var mappedSlots = _mapper.Map<List<ScheduleSlotDTO>>(slots);
                
                // ✅ Tính BookedCount tự động
                await CalculateBookedCountForSlots(mappedSlots);
                
                // Gán SlotNumber theo thứ tự
                for (int i = 0; i < mappedSlots.Count; i++)
                {
                    mappedSlots[i].SlotNumber = i + 1;
                }
                
                return mappedSlots;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách slots theo facility: {FacilityId}", facilityId);
                throw;
            }
        }

        public async Task<ScheduleSlotDTO> GetSlotByIdAsync(int slotId)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<ScheduleSlot>();
                var slot = await repository.GetAsync(s => s.SlotId == slotId, includeProperties: "Facility");
                
                if (slot == null)
                {
                    throw new ArgumentException($"Slot với ID {slotId} không tồn tại");
                }
                
                var mappedSlot = _mapper.Map<ScheduleSlotDTO>(slot);
                
                // ✅ Tính BookedCount tự động
                await CalculateBookedCountForSlots(new List<ScheduleSlotDTO> { mappedSlot });
                
                // Tính SlotNumber dựa trên vị trí trong danh sách
                var allSlotsResult = await repository.GetAllAsync(null, null, "", null, null);
                var allSlots = allSlotsResult.Data;
                var orderedSlots = allSlots.OrderBy(s => s.SlotTime).ToList();
                mappedSlot.SlotNumber = orderedSlots.FindIndex(s => s.SlotId == slotId) + 1;
                
                return mappedSlot;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy slot theo ID: {SlotId}", slotId);
                throw;
            }
        }

        // ✅ Thêm method GetSlotByIdWithFacilityCheckAsync cho phân quyền
        public async Task<ScheduleSlotDTO> GetSlotByIdWithFacilityCheckAsync(int slotId, int facilityId)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<ScheduleSlot>();
                var slot = await repository.GetAsync(s => s.SlotId == slotId && s.FacilityId == facilityId, includeProperties: "Facility");

                if (slot == null)
                {
                    throw new ArgumentException($"Slot với ID {slotId} không tồn tại hoặc không thuộc facility của bạn");
                }

                var mappedSlot = _mapper.Map<ScheduleSlotDTO>(slot);
                
                // ✅ Tính BookedCount tự động
                await CalculateBookedCountForSlots(new List<ScheduleSlotDTO> { mappedSlot });
                
                // Tính SlotNumber dựa trên vị trí trong danh sách facility
                var allSlotsResult = await repository.GetAllAsync(null, null, "", null, null);
                var allSlots = allSlotsResult.Data;
                var facilitySlots = allSlots.Where(s => s.FacilityId == facilityId).OrderBy(s => s.SlotTime).ToList();
                mappedSlot.SlotNumber = facilitySlots.FindIndex(s => s.SlotId == slotId) + 1;
                
                return mappedSlot;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy slot theo ID với facility check: {SlotId}, {FacilityId}", slotId, facilityId);
                throw;
            }
        }

        // ✅ Helper method để tính BookedCount tự động
        private async Task CalculateBookedCountForSlots(List<ScheduleSlotDTO> slots)
        {
            var appointmentRepo = _unitOfWork.GetRepository<AppointmentSchedule>();
            var appointmentsResult = await appointmentRepo.GetAllAsync(null, null, "", null, null);
            var allAppointments = appointmentsResult.Data;
            
            foreach (var slot in slots)
            {
                // Đếm số appointments cho slot này
                var bookedCount = allAppointments.Count(a => a.SlotId == slot.SlotId);
                slot.BookedCount = bookedCount;
                slot.AvailableCapacity = slot.MaxCapacity - slot.BookedCount;
            }
        }

        // ✅ Cập nhật CreateSlotAsync với FacilityId
        public async Task<List<ScheduleSlotDTO>> CreateSlotAsync(CreateScheduleSlotDTO createDto, int facilityId)
        {
            try
            {
                if (createDto.IsWorkingHours)
                {
                    // ✅ TẠO WORKING HOURS (multiple slots)
                    return await CreateWorkingHoursSlotsAsync(createDto, facilityId);
                }
                else
                {
                    // ✅ TẠO SINGLE SLOT
                    var singleSlot = await CreateSingleSlotAsync(createDto, facilityId);
                    return new List<ScheduleSlotDTO> { singleSlot };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo slot cho facility: {FacilityId}", facilityId);
                throw;
            }
        }

        private async Task<ScheduleSlotDTO> CreateSingleSlotAsync(CreateScheduleSlotDTO createDto, int facilityId)
        {
            _logger.LogInformation("Tạo slot đơn lẻ cho facility: {FacilityId}", facilityId);

            var slot = _mapper.Map<ScheduleSlot>(createDto);
            // ✅ BookedCount luôn bắt đầu từ 0
            slot.BookedCount = 0;
            // ✅ Set FacilityId từ JWT token
            slot.FacilityId = facilityId;

            var repository = _unitOfWork.GetRepository<ScheduleSlot>();
            await repository.AddAsync(slot);
            await _unitOfWork.SaveChangesAsync();

            var mappedSlot = _mapper.Map<ScheduleSlotDTO>(slot);
            
            // ✅ Tính BookedCount tự động (sẽ là 0 vì mới tạo)
            await CalculateBookedCountForSlots(new List<ScheduleSlotDTO> { mappedSlot });
            
            // Tính SlotNumber sau khi tạo
            var allSlotsResult = await repository.GetAllAsync(null, null, "", null, null);
            var allSlots = allSlotsResult.Data;
            var facilitySlots = allSlots.Where(s => s.FacilityId == facilityId).OrderBy(s => s.SlotTime).ToList();
            mappedSlot.SlotNumber = facilitySlots.FindIndex(s => s.SlotId == slot.SlotId) + 1;

            _logger.LogInformation("Tạo slot đơn lẻ thành công cho facility: {FacilityId}", facilityId);
            return mappedSlot;
        }

        private async Task<List<ScheduleSlotDTO>> CreateWorkingHoursSlotsAsync(CreateScheduleSlotDTO createDto, int facilityId)
        {
            _logger.LogInformation("Tạo working hours từ {StartTime} đến {EndTime} cho facility: {FacilityId}", 
                createDto.StartTime, createDto.EndTime, facilityId);

            // Validation
            if (!createDto.StartTime.HasValue || !createDto.EndTime.HasValue || !createDto.SlotDurationMinutes.HasValue)
            {
                throw new ArgumentException("StartTime, EndTime và SlotDurationMinutes là bắt buộc cho working hours");
            }

            var slots = new List<ScheduleSlot>();
            var currentTime = createDto.StartTime.Value;

            while (currentTime < createDto.EndTime.Value)
            {
                var slotEndTime = currentTime.AddMinutes(createDto.SlotDurationMinutes.Value);
                
                // Kiểm tra lunch break
                if (createDto.LunchBreakStart.HasValue && createDto.LunchBreakEnd.HasValue)
                {
                    if (currentTime >= createDto.LunchBreakStart.Value && currentTime < createDto.LunchBreakEnd.Value)
                    {
                        currentTime = createDto.LunchBreakEnd.Value;
                        continue;
                    }
                }

                if (slotEndTime <= createDto.EndTime.Value)
                {
                    var slot = new ScheduleSlot
                    {
                        // ✅ Working hours không cần SlotTime, để null
                        SlotTime = null,
                        
                        // ✅ Working Hours Config theo entity mới
                        StartTime = createDto.StartTime.Value,
                        EndTime = createDto.EndTime.Value, 
                        SlotDurationMinutes = createDto.SlotDurationMinutes.Value,
                        LunchBreakStart = createDto.LunchBreakStart,
                        LunchBreakEnd = createDto.LunchBreakEnd,
                        
                        MaxCapacity = createDto.MaxCapacity,
                        BookedCount = 0, // ✅ Luôn bắt đầu từ 0
                        Status = createDto.Status,
                        IsWorkingHours = true,
                        FacilityId = facilityId, // ✅ Set FacilityId
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    slots.Add(slot);
                }

                currentTime = slotEndTime;
            }

            var repository = _unitOfWork.GetRepository<ScheduleSlot>();
            await repository.AddRangeAsync(slots);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Tạo working hours thành công với {Count} slots cho facility: {FacilityId}", 
                slots.Count, facilityId);
            
            // ✅ Return tất cả slots đã tạo
            var mappedSlots = _mapper.Map<List<ScheduleSlotDTO>>(slots);
            
            // ✅ Tính BookedCount tự động (sẽ là 0 vì mới tạo)
            await CalculateBookedCountForSlots(mappedSlots);
            
            // Gán SlotNumber
            for (int i = 0; i < mappedSlots.Count; i++)
            {
                mappedSlots[i].SlotNumber = i + 1;
            }

            return mappedSlots;
        }

        public async Task<ScheduleSlotDTO> UpdateSlotAsync(int slotId, UpdateScheduleSlotDTO updateDto)
        {
            try
            {
                _logger.LogInformation("Cập nhật slot ID: {SlotId}", slotId);

                var repository = _unitOfWork.GetRepository<ScheduleSlot>();
                var slot = await repository.GetAsync(s => s.SlotId == slotId);

                if (slot == null)
                {
                    throw new ArgumentException($"Slot với ID {slotId} không tồn tại");
                }

                _mapper.Map(updateDto, slot);
                repository.Update(slot);
                await _unitOfWork.SaveChangesAsync();

                var mappedSlot = _mapper.Map<ScheduleSlotDTO>(slot);
                
                // ✅ Tính BookedCount tự động
                await CalculateBookedCountForSlots(new List<ScheduleSlotDTO> { mappedSlot });
                
                // Tính SlotNumber sau khi cập nhật
                var allSlots = await repository.GetAllAsync("");
                var orderedSlots = allSlots.OrderBy(s => s.SlotTime).ToList();
                mappedSlot.SlotNumber = orderedSlots.FindIndex(s => s.SlotId == slotId) + 1;

                _logger.LogInformation("Cập nhật slot thành công");
                return mappedSlot;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật slot ID: {SlotId}", slotId);
                throw;
            }
        }

        public async Task<bool> DeleteSlotAsync(int slotId)
        {
            try
            {
                _logger.LogInformation("Xóa slot ID: {SlotId}", slotId);

                var repository = _unitOfWork.GetRepository<ScheduleSlot>();
                var slot = await repository.GetAsync(s => s.SlotId == slotId);

                if (slot == null)
                {
                    throw new ArgumentException($"Slot với ID {slotId} không tồn tại");
                }

                repository.Delete(slot);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Xóa slot thành công");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa slot ID: {SlotId}", slotId);
                throw;
            }
        }

        public async Task<bool> DeleteWorkingHoursAsync(TimeOnly startTime, TimeOnly endTime)
        {
            try
            {
                _logger.LogInformation("Xóa working hours: {StartTime} - {EndTime}", startTime, endTime);

                var repository = _unitOfWork.GetRepository<ScheduleSlot>();
                var allSlots = await repository.GetAllAsync("");
                
                var slotsToDelete = allSlots.Where(s => s.IsWorkingHours && 
                                                      s.StartTime == startTime && 
                                                      s.EndTime == endTime)
                                           .ToList();

                if (slotsToDelete.Any())
                {
                    foreach (var slot in slotsToDelete)
                    {
                        repository.Delete(slot);
                    }
                    await _unitOfWork.SaveChangesAsync();
                }

                _logger.LogInformation("Xóa working hours thành công: {Count} slots", slotsToDelete.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa working hours");
                throw;
            }
        }

        public async Task<List<ScheduleSlotDTO>> UpdateWorkingHoursAsync(TimeOnly oldStartTime, TimeOnly oldEndTime, CreateScheduleSlotDTO newConfig, int facilityId)
        {
            try
            {
                _logger.LogInformation("Cập nhật working hours từ {OldStart}-{OldEnd} thành {NewStart}-{NewEnd} cho facility: {FacilityId}", 
                    oldStartTime, oldEndTime, newConfig.StartTime, newConfig.EndTime, facilityId);

                // 1. Xóa working hours cũ
                await DeleteWorkingHoursAsync(oldStartTime, oldEndTime);
                
                // 2. Tạo working hours mới
                var newSlots = await CreateSlotAsync(newConfig, facilityId);

                _logger.LogInformation("Cập nhật working hours thành công cho facility: {FacilityId}", facilityId);
                return newSlots;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật working hours cho facility: {FacilityId}", facilityId);
                throw;
            }
        }

        public async Task<List<ScheduleSlotDTO>> GetWorkingHoursSlotsAsync(TimeOnly startTime, TimeOnly endTime)
        {
            try
            {
                _logger.LogInformation("Lấy working hours slots: {StartTime} - {EndTime}", startTime, endTime);

                var repository = _unitOfWork.GetRepository<ScheduleSlot>();
                var allSlots = await repository.GetAllAsync("");
                
                var slots = allSlots.Where(s => s.IsWorkingHours && 
                                               s.StartTime == startTime && 
                                               s.EndTime == endTime)
                                   .OrderBy(s => s.CreatedAt)
                                   .ToList();

                var mappedSlots = _mapper.Map<List<ScheduleSlotDTO>>(slots);
                
                // ✅ Tính BookedCount tự động
                await CalculateBookedCountForSlots(mappedSlots);
                
                for (int i = 0; i < mappedSlots.Count; i++)
                {
                    mappedSlots[i].SlotNumber = i + 1;
                }

                return mappedSlots;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy working hours slots");
                throw;
            }
        }

        public async Task<bool> UpdateSlotStatusAsync(int slotId, string status)
        {
            try
            {
                _logger.LogInformation("Cập nhật trạng thái slot ID: {SlotId} thành {Status}", slotId, status);

                var repository = _unitOfWork.GetRepository<ScheduleSlot>();
                var slot = await repository.GetAsync(s => s.SlotId == slotId);

                if (slot == null)
                {
                    throw new ArgumentException($"Slot với ID {slotId} không tồn tại");
                }

                slot.Status = status;
                slot.UpdatedAt = DateTime.UtcNow;

                repository.Update(slot);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Cập nhật trạng thái slot thành công");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật trạng thái slot ID: {SlotId}", slotId);
                throw;
            }
        }

        public async Task<bool> DeleteMultipleSlotsAsync(List<int> slotIds)
        {
            try
            {
                _logger.LogInformation("Xóa nhiều slots: {SlotIds}", string.Join(", ", slotIds));

                var repository = _unitOfWork.GetRepository<ScheduleSlot>();
                var slotsResult = await repository.GetAllAsync(null, null, "", null, null);
                var allSlots = slotsResult.Data;
                var slotsToDelete = allSlots.Where(s => slotIds.Contains(s.SlotId)).ToList();

                if (slotsToDelete.Any())
                {
                    foreach (var slot in slotsToDelete)
                    {
                        repository.Delete(slot);
                    }
                    await _unitOfWork.SaveChangesAsync();
                }

                _logger.LogInformation("Xóa nhiều slots thành công");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa nhiều slots");
                throw;
            }
        }
    }
} 