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
                    src.MaxCapacity - src.BookedCount));

            // Mapping từ CreateDTO sang Entity
            CreateMap<CreateScheduleSlotDTO, ScheduleSlot>()
                .ForMember(dest => dest.SlotId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.AppointmentSchedules, opt => opt.Ignore());

            // Mapping từ UpdateDTO sang Entity
            CreateMap<UpdateScheduleSlotDTO, ScheduleSlot>()
                .ForMember(dest => dest.SlotId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.AppointmentSchedules, opt => opt.Ignore());
        }
    }
} 