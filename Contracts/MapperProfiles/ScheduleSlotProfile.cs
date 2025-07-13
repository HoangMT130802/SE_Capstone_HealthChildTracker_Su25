using AutoMapper;
using Contracts.DTOs.FacilitySchedule;
using Repositories.Entities;

namespace Contracts.MapperProfiles
{
    public class ScheduleSlotProfile : Profile
    {
        public ScheduleSlotProfile()
        {
            // Mapping từ Entity sang DTO
            CreateMap<ScheduleSlot, ScheduleSlotDTO>()
                .ForMember(dest => dest.AvailableCapacity, opt => opt.Ignore()) // Sẽ được tính trong service
                .ForMember(dest => dest.BookedCount, opt => opt.Ignore()) // Sẽ được tính trong service
                .ForMember(dest => dest.SlotNumber, opt => opt.Ignore()) // Sẽ được set trong service
                .ForMember(dest => dest.FacilityName, opt => opt.MapFrom(src => src.Facility != null ? src.Facility.FacilityName : null)); // ✅ Map facility name

            // Mapping từ CreateDTO sang Entity
            CreateMap<CreateScheduleSlotDTO, ScheduleSlot>()
                .ForMember(dest => dest.SlotId, opt => opt.Ignore())
                .ForMember(dest => dest.BookedCount, opt => opt.Ignore()) // ✅ Sẽ được set manual trong service = 0
                .ForMember(dest => dest.FacilityId, opt => opt.Ignore()) // ✅ Sẽ được set manual trong service từ JWT
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.AppointmentSchedules, opt => opt.Ignore())
                .ForMember(dest => dest.Facility, opt => opt.Ignore()) // ✅ Navigation property
                // ✅ Handle working hours fields - sẽ được set manual trong service cho working hours
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.IsWorkingHours ? src.StartTime : null))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.IsWorkingHours ? src.EndTime : null))
                .ForMember(dest => dest.SlotDurationMinutes, opt => opt.MapFrom(src => src.IsWorkingHours ? src.SlotDurationMinutes : null))
                .ForMember(dest => dest.LunchBreakStart, opt => opt.MapFrom(src => src.IsWorkingHours ? src.LunchBreakStart : null))
                .ForMember(dest => dest.LunchBreakEnd, opt => opt.MapFrom(src => src.IsWorkingHours ? src.LunchBreakEnd : null));

            // Mapping từ UpdateDTO sang Entity (chỉ cho single slots)
            CreateMap<UpdateScheduleSlotDTO, ScheduleSlot>()
                .ForMember(dest => dest.SlotId, opt => opt.Ignore())
                .ForMember(dest => dest.BookedCount, opt => opt.Ignore()) // ✅ Không update BookedCount
                .ForMember(dest => dest.FacilityId, opt => opt.Ignore()) // ✅ Không update FacilityId
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.AppointmentSchedules, opt => opt.Ignore())
                .ForMember(dest => dest.Facility, opt => opt.Ignore()) // ✅ Navigation property
                // ✅ Working hours fields không được update từ UpdateDTO
                .ForMember(dest => dest.StartTime, opt => opt.Ignore())
                .ForMember(dest => dest.EndTime, opt => opt.Ignore())
                .ForMember(dest => dest.SlotDurationMinutes, opt => opt.Ignore())
                .ForMember(dest => dest.LunchBreakStart, opt => opt.Ignore())
                .ForMember(dest => dest.LunchBreakEnd, opt => opt.Ignore())
                .ForMember(dest => dest.IsWorkingHours, opt => opt.Ignore());
        }
    }
} 