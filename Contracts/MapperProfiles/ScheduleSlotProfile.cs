using AutoMapper;
using Contracts.DTOs.FacilitySchedule;
using Repositories.Entities;

namespace Contracts.MapperProfiles
{
    public class ScheduleSlotProfile : Profile
    {
        public ScheduleSlotProfile()
        {
            // ✅ Entity to DTO
            CreateMap<ScheduleSlot, ScheduleSlotDTO>()
                .ForMember(dest => dest.FacilityName, opt => opt.MapFrom(src => src.Facility.FacilityName))
                .ForMember(dest => dest.WorkingHoursGroupId, opt => opt.MapFrom(src => src.WorkingHoursGroupId))
                .ForMember(dest => dest.SlotTime, opt => opt.MapFrom(src => src.SlotTime))
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.StartTime ?? TimeOnly.MinValue))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.EndTime ?? TimeOnly.MinValue))
                .ForMember(dest => dest.SlotDurationMinutes, opt => opt.MapFrom(src => src.SlotDurationMinutes ?? 0))
                .ForMember(dest => dest.LunchBreakStart, opt => opt.MapFrom(src => src.LunchBreakStart))
                .ForMember(dest => dest.LunchBreakEnd, opt => opt.MapFrom(src => src.LunchBreakEnd))
                .ForMember(dest => dest.IsWorkingHours, opt => opt.MapFrom(src => src.IsWorkingHours))
                .ForMember(dest => dest.SlotNumber, opt => opt.Ignore()) // Sẽ được set trong service
                .ForMember(dest => dest.AvailableCapacity, opt => opt.Ignore()); // Computed property

            // ✅ CreateDTO to Entity - Map với nullable fields
            CreateMap<CreateScheduleSlotDTO, ScheduleSlot>()
                .ForMember(dest => dest.SlotId, opt => opt.Ignore()) // Auto-generated
                .ForMember(dest => dest.FacilityId, opt => opt.Ignore()) // Set từ JWT token
                .ForMember(dest => dest.WorkingHoursGroupId, opt => opt.Ignore()) // Set trong service
                .ForMember(dest => dest.SlotTime, opt => opt.Ignore()) // Sẽ được set trong service
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.StartTime))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.EndTime))
                .ForMember(dest => dest.SlotDurationMinutes, opt => opt.MapFrom(src => src.SlotDurationMinutes))
                .ForMember(dest => dest.LunchBreakStart, opt => opt.MapFrom(src => src.LunchBreakStart))
                .ForMember(dest => dest.LunchBreakEnd, opt => opt.MapFrom(src => src.LunchBreakEnd))
                .ForMember(dest => dest.MaxCapacity, opt => opt.MapFrom(src => src.MaxCapacity))
                .ForMember(dest => dest.BookedCount, opt => opt.MapFrom(src => 0)) // Luôn bắt đầu từ 0
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.IsWorkingHours, opt => opt.MapFrom(src => src.IsWorkingHours))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Facility, opt => opt.Ignore()) // Navigation property
                .ForMember(dest => dest.AppointmentSchedules, opt => opt.Ignore()); // Navigation property

            // ✅ UpdateDTO to Entity (nếu cần)
            CreateMap<UpdateScheduleSlotDTO, ScheduleSlot>()
                .ForMember(dest => dest.SlotId, opt => opt.Ignore())
                .ForMember(dest => dest.FacilityId, opt => opt.Ignore())
                .ForMember(dest => dest.WorkingHoursGroupId, opt => opt.Ignore())
                .ForMember(dest => dest.SlotTime, opt => opt.Ignore()) // Không update SlotTime
                .ForMember(dest => dest.StartTime, opt => opt.Ignore()) // Không update StartTime
                .ForMember(dest => dest.EndTime, opt => opt.Ignore()) // Không update EndTime
                .ForMember(dest => dest.SlotDurationMinutes, opt => opt.Ignore()) // Không update duration
                .ForMember(dest => dest.LunchBreakStart, opt => opt.Ignore()) // Không update lunch break
                .ForMember(dest => dest.LunchBreakEnd, opt => opt.Ignore()) // Không update lunch break
                .ForMember(dest => dest.IsWorkingHours, opt => opt.Ignore()) // Không update IsWorkingHours
                .ForMember(dest => dest.BookedCount, opt => opt.Ignore()) // Không update BookedCount
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Facility, opt => opt.Ignore())
                .ForMember(dest => dest.AppointmentSchedules, opt => opt.Ignore());
        }
    }
} 