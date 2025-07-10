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
                .ForMember(dest => dest.AvailableCapacity, opt => opt.MapFrom(src => 
                    src.MaxCapacity - src.BookedCount))
                .ForMember(dest => dest.SlotNumber, opt => opt.Ignore()); // Sẽ được set trong service

            // Mapping từ CreateDTO sang Entity
            CreateMap<CreateScheduleSlotDTO, ScheduleSlot>()
                .ForMember(dest => dest.SlotId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.AppointmentSchedules, opt => opt.Ignore())
                // ✅ Handle working hours fields - sẽ được set manual trong service cho working hours
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.IsWorkingHours ? src.StartTime : null))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.IsWorkingHours ? src.EndTime : null))
                .ForMember(dest => dest.SlotDurationMinutes, opt => opt.MapFrom(src => src.IsWorkingHours ? src.SlotDurationMinutes : null))
                .ForMember(dest => dest.LunchBreakStart, opt => opt.MapFrom(src => src.IsWorkingHours ? src.LunchBreakStart : null))
                .ForMember(dest => dest.LunchBreakEnd, opt => opt.MapFrom(src => src.IsWorkingHours ? src.LunchBreakEnd : null));

            // Mapping từ UpdateDTO sang Entity (chỉ cho single slots)
            CreateMap<UpdateScheduleSlotDTO, ScheduleSlot>()
                .ForMember(dest => dest.SlotId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.AppointmentSchedules, opt => opt.Ignore())
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