using AutoMapper;
using Contracts.DTOs.FacilitySchedule;
using Microsoft.Extensions.Logging;
using Repositories.Common;
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
                
                var result = slots.OrderBy(s => s.StartTime).ToList();
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

        public async Task<List<ScheduleSlotDTO>> GetSlotsByFacilityAsync(int facilityId)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<ScheduleSlot>();
                var allSlotsResult = await repository.GetAllAsync(null, null, "Facility", null, null);
                var allSlots = allSlotsResult.Data;
                var slots = allSlots.Where(s => s.FacilityId == facilityId).OrderBy(s => s.StartTime).ToList();
                
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
                var slot = await repository.GetAsync(s => s.SlotId == slotId, "Facility");
                
                if (slot == null)
                {
                    throw new ArgumentException($"Không tìm thấy slot với ID: {slotId}");
                }
                
                var mappedSlot = _mapper.Map<ScheduleSlotDTO>(slot);
                
                // ✅ Tính BookedCount tự động
                await CalculateBookedCountForSlots(new List<ScheduleSlotDTO> { mappedSlot });
                
                return mappedSlot;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy slot theo ID: {SlotId}", slotId);
                throw;
            }
        }

        public async Task<ScheduleSlotDTO> GetSlotByIdWithFacilityCheckAsync(int slotId, int facilityId)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<ScheduleSlot>();
                var slot = await repository.GetAsync(s => s.SlotId == slotId, "Facility");
                
                if (slot == null)
                {
                    throw new ArgumentException($"Không tìm thấy slot với ID: {slotId}");
                }
                
                if (slot.FacilityId != facilityId)
                {
                    throw new UnauthorizedAccessException($"Slot {slotId} không thuộc về facility {facilityId}");
                }
                
                var mappedSlot = _mapper.Map<ScheduleSlotDTO>(slot);
                
                // ✅ Tính BookedCount tự động
                await CalculateBookedCountForSlots(new List<ScheduleSlotDTO> { mappedSlot });
                
                return mappedSlot;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy slot theo ID với facility check: {SlotId}, {FacilityId}", slotId, facilityId);
                throw;
            }
        }

        // ✅ Tính BookedCount tự động từ AppointmentSchedule
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
            }
        }

        public async Task<List<ScheduleSlotDTO>> CreateSlotAsync(CreateScheduleSlotDTO createDto, int facilityId)
        {
            try
            {
                // ✅ Validation
                if (!createDto.IsValid())
                {
                    throw new ArgumentException("Dữ liệu đầu vào không hợp lệ");
                }

                // ✅ Chỉ tạo working hours (multiple slots)
                return await CreateWorkingHoursSlotsAsync(createDto, facilityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo working hours slots cho facility: {FacilityId}", facilityId);
                throw;
            }
        }



        private async Task<List<ScheduleSlotDTO>> CreateWorkingHoursSlotsAsync(CreateScheduleSlotDTO createDto, int facilityId)
        {
            _logger.LogInformation("Tạo working hours từ {StartTime} đến {EndTime} cho facility: {FacilityId}", 
                createDto.StartTime, createDto.EndTime, facilityId);

            // ✅ Generate WorkingHoursGroupId cho tất cả slots
            var workingHoursGroupId = GenerateWorkingHoursGroupId(facilityId);

            var slots = new List<ScheduleSlot>();
            var currentTime = createDto.StartTime;
            var endTime = createDto.EndTime;
            var slotDuration = createDto.SlotDurationMinutes;

            while (currentTime < endTime)
            {
                var slotEndTime = currentTime.AddMinutes(slotDuration);
                
                // ✅ Kiểm tra lunch break
                if (createDto.LunchBreakStart.HasValue && createDto.LunchBreakEnd.HasValue)
                {
                    // Nếu slot này overlap với lunch break thì skip
                    if (currentTime >= createDto.LunchBreakStart.Value && currentTime < createDto.LunchBreakEnd.Value)
                    {
                        currentTime = createDto.LunchBreakEnd.Value;
                        continue;
                    }
                }

                // ✅ Chỉ tạo slot nếu không vượt quá thời gian kết thúc
                if (slotEndTime <= endTime)
                {
                    // ✅ Tạo SlotTime string format "08:00 - 09:00"
                    var slotTimeString = $"{currentTime:HH:mm} - {slotEndTime:HH:mm}";

                    var slot = new ScheduleSlot
                    {
                        FacilityId = facilityId,
                        WorkingHoursGroupId = workingHoursGroupId, // ✅ Assign cùng GroupId
                        SlotTime = slotTimeString, // ✅ Set SlotTime cho frontend
                        StartTime = currentTime,
                        EndTime = slotEndTime,
                        SlotDurationMinutes = slotDuration,
                        LunchBreakStart = createDto.LunchBreakStart,
                        LunchBreakEnd = createDto.LunchBreakEnd,
                        MaxCapacity = createDto.MaxCapacity,
                        BookedCount = 0, // Luôn bắt đầu từ 0
                        Status = createDto.Status,
                        IsWorkingHours = true, // ✅ Luôn là working hours
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    slots.Add(slot);
                }

                currentTime = slotEndTime;
            }

            // ✅ Lưu tất cả slots vào database
            if (slots.Count > 0)
            {
                var repository = _unitOfWork.GetRepository<ScheduleSlot>();
                foreach (var slot in slots)
                {
                    await repository.AddAsync(slot);
                }
                await _unitOfWork.SaveChangesAsync();
            }

            // ✅ Map và tính BookedCount
            var mappedSlots = _mapper.Map<List<ScheduleSlotDTO>>(slots);
            await CalculateBookedCountForSlots(mappedSlots);

            _logger.LogInformation("Tạo {Count} working hours slots với GroupId {GroupId} cho facility: {FacilityId}", 
                mappedSlots.Count, workingHoursGroupId, facilityId);

            return mappedSlots;
        }

        // ✅ Generate WorkingHoursGroupId
        private string GenerateWorkingHoursGroupId(int facilityId)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var randomSuffix = Guid.NewGuid().ToString()[..8];
            return $"WH_{facilityId}_{timestamp}_{randomSuffix}";
        }

        // ✅ Get slots by WorkingHoursGroupId
        public async Task<List<ScheduleSlotDTO>> GetSlotsByWorkingHoursGroupIdAsync(string workingHoursGroupId)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<ScheduleSlot>();
                var result = await repository.GetAllAsync(
                    filter: s => s.WorkingHoursGroupId == workingHoursGroupId,
                    orderBy: q => q.OrderBy(s => s.StartTime),
                    include: "Facility"
                );
                
                var mappedSlots = _mapper.Map<List<ScheduleSlotDTO>>(result.Data);
                await CalculateBookedCountForSlots(mappedSlots);

                return mappedSlots;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy slots theo WorkingHoursGroupId: {GroupId}", workingHoursGroupId);
                throw;
            }
        }

        // ✅ Get working hours groups by facility
        public async Task<List<WorkingHoursGroupDTO>> GetWorkingHoursGroupsByFacilityAsync(int facilityId)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<ScheduleSlot>();
                var result = await repository.GetAllAsync(
                    filter: s => s.FacilityId == facilityId && !string.IsNullOrEmpty(s.WorkingHoursGroupId),
                    orderBy: q => q.OrderByDescending(s => s.CreatedAt),
                    include: "Facility"
                );

                var groups = result.Data.GroupBy(s => s.WorkingHoursGroupId)
                                        .Select(g => new WorkingHoursGroupDTO
                                        {
                                            GroupId = g.Key,
                                            Description = GenerateWorkingHoursDescription(g.First()),
                                            TotalSlots = g.Count(),
                                            StartTime = g.Min(s => s.StartTime.Value),
                                            EndTime = g.Max(s => s.EndTime.Value),
                                            SlotDurationMinutes = g.First().SlotDurationMinutes.Value,
                                            LunchBreakStart = g.First().LunchBreakStart,
                                            LunchBreakEnd = g.First().LunchBreakEnd,
                                            CreatedAt = g.First().CreatedAt,
                                            Slots = _mapper.Map<List<ScheduleSlotDTO>>(g.OrderBy(s => s.StartTime).ToList())
                                        })
                                        .OrderByDescending(g => g.CreatedAt)
                                        .ToList();

                return groups;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy working hours groups cho facility: {FacilityId}", facilityId);
                throw;
            }
        }

        // ✅ Generate description for working hours group
        private string GenerateWorkingHoursDescription(ScheduleSlot slot)
        {
            var lunchInfo = slot.LunchBreakStart.HasValue && slot.LunchBreakEnd.HasValue
                ? $" (Lunch: {slot.LunchBreakStart.Value:HH:mm}-{slot.LunchBreakEnd.Value:HH:mm})"
                : "";
            
            return $"Working Hours {slot.StartTime.Value:HH:mm}-{slot.EndTime.Value:HH:mm} - {slot.SlotDurationMinutes}min slots{lunchInfo}";
        }

        public async Task<ScheduleSlotDTO> UpdateSlotAsync(int slotId, UpdateScheduleSlotDTO updateDto, int facilityId)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<ScheduleSlot>();
                var slot = await repository.GetByIdAsync(slotId);
                
                if (slot == null)
                {
                    throw new ArgumentException($"Không tìm thấy slot với ID: {slotId}");
                }
                
                if (slot.FacilityId != facilityId)
                {
                    throw new UnauthorizedAccessException($"Slot {slotId} không thuộc về facility {facilityId}");
                }
                
                // ✅ Cập nhật thông tin slot (chỉ những field cho phép)
                _mapper.Map(updateDto, slot);
                
                repository.Update(slot);
                await _unitOfWork.SaveChangesAsync();
                
                var mappedSlot = _mapper.Map<ScheduleSlotDTO>(slot);
                
                // ✅ Tính BookedCount tự động
                await CalculateBookedCountForSlots(new List<ScheduleSlotDTO> { mappedSlot });
                
                return mappedSlot;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật slot: {SlotId}", slotId);
                throw;
            }
        }

        public async Task<bool> DeleteSlotAsync(int slotId, int facilityId)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<ScheduleSlot>();
                var slot = await repository.GetByIdAsync(slotId);
                
                if (slot == null)
                {
                    throw new ArgumentException($"Không tìm thấy slot với ID: {slotId}");
                }
                
                if (slot.FacilityId != facilityId)
                {
                    throw new UnauthorizedAccessException($"Slot {slotId} không thuộc về facility {facilityId}");
                }
                
                repository.Delete(slot);
                await _unitOfWork.SaveChangesAsync();
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa slot: {SlotId}", slotId);
                throw;
            }
        }

        public async Task<bool> UpdateSlotStatusAsync(int slotId, string status)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<ScheduleSlot>();
                var slot = await repository.GetByIdAsync(slotId);
                
                if (slot == null)
                {
                    throw new ArgumentException($"Không tìm thấy slot với ID: {slotId}");
                }
                
                slot.Status = status;
                slot.UpdatedAt = DateTime.UtcNow;
                
                repository.Update(slot);
                await _unitOfWork.SaveChangesAsync();
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật trạng thái slot: {SlotId}", slotId);
                throw;
            }
        }

        public async Task<bool> DeleteMultipleSlotsAsync(List<int> slotIds, int facilityId)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<ScheduleSlot>();
                var deletedCount = 0;
                
                foreach (var slotId in slotIds)
                {
                    var slot = await repository.GetByIdAsync(slotId);
                    
                    if (slot != null && slot.FacilityId == facilityId)
                    {
                        repository.Delete(slot);
                        deletedCount++;
                    }
                }
                
                await _unitOfWork.SaveChangesAsync();
                
                _logger.LogInformation("Xóa multiple slots thành công: {Count}/{Total}", deletedCount, slotIds.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa multiple slots");
                throw;
            }
        }

        // ✅ Backward compatibility methods (có thể loại bỏ sau)
        public async Task<bool> DeleteWorkingHoursAsync(TimeOnly startTime, TimeOnly endTime)
        {
            // ✅ Deprecated method - có thể xóa sau
            _logger.LogWarning("DeleteWorkingHoursAsync is deprecated. Use DeleteMultipleSlotsAsync instead.");
            return true;
        }

        public async Task<List<ScheduleSlotDTO>> UpdateWorkingHoursAsync(TimeOnly oldStartTime, TimeOnly oldEndTime, CreateScheduleSlotDTO newConfig, int facilityId)
        {
            // ✅ Deprecated method - có thể xóa sau
            _logger.LogWarning("UpdateWorkingHoursAsync is deprecated. Delete and recreate working hours instead.");
            return await CreateSlotAsync(newConfig, facilityId);
        }

        public async Task<List<ScheduleSlotDTO>> GetWorkingHoursSlotsAsync(TimeOnly startTime, TimeOnly endTime)
        {
            // ✅ Deprecated method - có thể xóa sau
            _logger.LogWarning("GetWorkingHoursSlotsAsync is deprecated. Use GetSlotsByFacilityAsync instead.");
            return new List<ScheduleSlotDTO>();
        }
    }
} 