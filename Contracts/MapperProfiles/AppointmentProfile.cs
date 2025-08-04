using AutoMapper;
using Contracts.DTOs.Appointment;
using Repositories.Entities;

namespace Contracts.MapperProfiles
{
    public class AppointmentProfile : Profile
    {
        public AppointmentProfile()
        {
            // ✅ Mapping từ VaccinationAppointment sang AppointmentBookingResponseDTO
            CreateMap<VaccinationAppointment, AppointmentBookingResponseDTO>()
                .ForMember(dest => dest.AppointmentId, opt => opt.MapFrom(src => src.AppointmentId))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.Note, opt => opt.MapFrom(src => src.Note))
                .ForMember(dest => dest.Child, opt => opt.MapFrom(src => src.Child))
                .ForMember(dest => dest.Schedule, opt => opt.MapFrom(src => src.Schedule))
                .ForMember(dest => dest.Disease, opt => opt.Ignore()) // Sẽ được set trong service
                .ForMember(dest => dest.Facility, opt => opt.MapFrom(src => src.Schedule.Facility))
                .ForMember(dest => dest.Package, opt => opt.Ignore()) // Sẽ được set trong service
                .ForMember(dest => dest.EstimatedCost, opt => opt.Ignore()); // Sẽ được set trong service

            // ✅ Mapping từ AppointmentSchedule sang AppointmentScheduleDTO
            CreateMap<AppointmentSchedule, AppointmentScheduleDTO>()
                .ForMember(dest => dest.Facility, opt => opt.MapFrom(src => src.Facility))
                .ForMember(dest => dest.Slot, opt => opt.MapFrom(src => src.Slot))
                .ForMember(dest => dest.FacilityName, opt => opt.MapFrom(src => src.Facility.FacilityName))
                .ForMember(dest => dest.SlotTime, opt => opt.MapFrom(src => src.Slot.SlotTime))
                .ForMember(dest => dest.MaxCapacity, opt => opt.MapFrom(src => src.Slot.MaxCapacity))
                .ForMember(dest => dest.AvailableSlots, opt => opt.MapFrom(src => 
                    src.Slot.MaxCapacity - (src.BookedCount ?? 0)));

            // ✅ Mapping từ AppointmentBookingRequestDTO sang VaccinationAppointment
            CreateMap<AppointmentBookingRequestDTO, VaccinationAppointment>()
                .ForMember(dest => dest.AppointmentId, opt => opt.Ignore())
                .ForMember(dest => dest.ChildId, opt => opt.MapFrom(src => src.ChildId))
                .ForMember(dest => dest.ScheduleId, opt => opt.MapFrom(src => src.ScheduleId))
                .ForMember(dest => dest.Note, opt => opt.MapFrom(src => src.Note))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => "Pending"))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.OrderId, opt => opt.Ignore()) // Sẽ được set sau khi tạo order
                .ForMember(dest => dest.Child, opt => opt.Ignore())
                .ForMember(dest => dest.Schedule, opt => opt.Ignore())
                .ForMember(dest => dest.Order, opt => opt.Ignore())
                .ForMember(dest => dest.AppointmentSurveys, opt => opt.Ignore())
                .ForMember(dest => dest.ChildVaccineProfiles, opt => opt.Ignore())
                .ForMember(dest => dest.VaccinationAppointmentDetails, opt => opt.Ignore());

            // ✅ Mapping cho ScheduleSlot + AppointmentSchedule sang AvailableSlotDTO
            CreateMap<AppointmentSchedule, AvailableSlotDTO>()
                .ForMember(dest => dest.ScheduleId, opt => opt.MapFrom(src => src.ScheduleId))
                .ForMember(dest => dest.SlotId, opt => opt.MapFrom(src => src.SlotId))
                .ForMember(dest => dest.SlotTime, opt => opt.MapFrom(src => src.Slot.SlotTime))
                .ForMember(dest => dest.MaxCapacity, opt => opt.MapFrom(src => src.Slot.MaxCapacity))
                .ForMember(dest => dest.BookedCount, opt => opt.MapFrom(src => src.BookedCount ?? 0))
                .ForMember(dest => dest.AvailableCapacity, opt => opt.MapFrom(src => src.Slot.MaxCapacity - (src.BookedCount ?? 0)))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status));

            // ✅ Mapping cho Rebooking DTOs
            CreateMap<VaccinationAppointment, AppointmentRebookingResponseDTO>()
                .ForMember(dest => dest.AppointmentId, opt => opt.MapFrom(src => src.AppointmentId))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.Note, opt => opt.MapFrom(src => src.Note))
                .ForMember(dest => dest.Child, opt => opt.Ignore()) // Set trong service
                .ForMember(dest => dest.Disease, opt => opt.Ignore()) // Set trong service
                .ForMember(dest => dest.Vaccine, opt => opt.Ignore()) // Set trong service
                .ForMember(dest => dest.DoseNumber, opt => opt.Ignore()) // Set trong service
                .ForMember(dest => dest.Schedule, opt => opt.Ignore()) // Set trong service
                .ForMember(dest => dest.EstimatedCost, opt => opt.Ignore()) // Set trong service
                .ForMember(dest => dest.UsedExistingOrder, opt => opt.Ignore()) // Set trong service
                .ForMember(dest => dest.UsedOrder, opt => opt.Ignore()) // Set trong service
                .ForMember(dest => dest.RemainingVaccinesInOrder, opt => opt.Ignore()) // Set trong service
                .ForMember(dest => dest.Message, opt => opt.Ignore()); // Set trong service

            // ✅ Mapping AppointmentSchedule cho rebooking
            CreateMap<AppointmentSchedule, AppointmentScheduleDTO>();

        }
    }
} 