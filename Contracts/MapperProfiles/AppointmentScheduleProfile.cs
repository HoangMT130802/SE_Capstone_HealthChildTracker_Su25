using AutoMapper;
using Contracts.DTOs.Appointment;
using Repositories.Entities;

namespace Contracts.MapperProfiles
{
    public class AppointmentScheduleProfile : Profile
    {
        public AppointmentScheduleProfile()
        {
            // Mapping từ Entity sang DTO
            CreateMap<AppointmentSchedule, AppointmentScheduleDTO>()
                .ForMember(dest => dest.FacilityName, opt => opt.MapFrom(src => src.Facility.FacilityName))
                .ForMember(dest => dest.SlotTime, opt => opt.MapFrom(src => src.Slot.SlotTime))
                .ForMember(dest => dest.MaxCapacity, opt => opt.MapFrom(src => src.Slot.MaxCapacity))
                .ForMember(dest => dest.AvailableSlots, opt => opt.MapFrom(src => 
                    src.Slot.MaxCapacity - (src.BookedCount ?? 0)));

            // Mapping từ CreateDTO sang Entity
            CreateMap<CreateAppointmentScheduleDTO, AppointmentSchedule>()
                .ForMember(dest => dest.ScheduleId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Facility, opt => opt.Ignore())
                .ForMember(dest => dest.Slot, opt => opt.Ignore())
                .ForMember(dest => dest.VaccinationAppointments, opt => opt.Ignore());

            // Mapping từ UpdateDTO sang Entity
            CreateMap<UpdateAppointmentScheduleDTO, AppointmentSchedule>()
                .ForMember(dest => dest.ScheduleId, opt => opt.Ignore())
                .ForMember(dest => dest.FacilityId, opt => opt.Ignore())
                .ForMember(dest => dest.SlotId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Facility, opt => opt.Ignore())
                .ForMember(dest => dest.Slot, opt => opt.Ignore())
                .ForMember(dest => dest.VaccinationAppointments, opt => opt.Ignore());
        }
    }
} 