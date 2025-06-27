using AutoMapper;
using Contracts.DTOs.ChildVaccineProfile;
using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.MapperProfiles
{
    public class ChildVaccineProfileMapper : Profile
    {
        public ChildVaccineProfileMapper()
        {
            CreateMap<ChildVaccineProfile, ChildVaccineProfileDTO>()
                .ForMember(dest => dest.VaccineProfileId, opt => opt.MapFrom(src => src.VaccineProfileId))
                .ForMember(dest => dest.ChildId, opt => opt.MapFrom(src => src.ChildId))
                .ForMember(dest => dest.AppointmentId, opt => opt.MapFrom(src => src.AppointmentId))
                .ForMember(dest => dest.VaccineId, opt => opt.MapFrom(src => src.VaccineId))
                .ForMember(dest => dest.DoseNum, opt => opt.MapFrom(src => src.DoseNum))
                .ForMember(dest => dest.ExpectedDate, opt => opt.MapFrom(src => src.ExpectedDate))
                .ForMember(dest => dest.ActualDate, opt => opt.MapFrom(src => src.ActualDate))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.IsRequired, opt => opt.MapFrom(src => src.IsRequired))
                .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt));
            CreateMap<CreateChildVaccineProfileDTO, ChildVaccineProfile>()
            .ForMember(dest => dest.ChildId, opt => opt.MapFrom(src => src.ChildId))
            .ForMember(dest => dest.VaccineId, opt => opt.MapFrom(src => src.VaccineId))
            .ForMember(dest => dest.AppointmentId, opt => opt.MapFrom(src => src.AppointmentId))
            .ForMember(dest => dest.DoseNum, opt => opt.MapFrom(src => src.DoseNum))
            .ForMember(dest => dest.ExpectedDate, opt => opt.MapFrom(src => src.ExpectedDate))
            .ForMember(dest => dest.ActualDate, opt => opt.MapFrom(src => src.ActualDate))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.IsRequired, opt => opt.MapFrom(src => src.IsRequired))
            .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority))
            .ForMember(dest => dest.VaccineProfileId, opt => opt.Ignore())
            .ForMember(dest => dest.AppointmentId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.Appointment, opt => opt.Ignore())
            .ForMember(dest => dest.Child, opt => opt.Ignore())
            .ForMember(dest => dest.Vaccine, opt => opt.Ignore());

            CreateMap<UpdateChildVaccineProfileDTO, ChildVaccineProfile>()
                .ForMember(dest => dest.ExpectedDate, opt => opt.MapFrom(src => src.ExpectedDate))
                .ForMember(dest => dest.ActualDate, opt => opt.MapFrom(src => src.ActualDate))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.IsRequired, opt => opt.MapFrom(src => src.IsRequired))
                .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority))
                .ForMember(dest => dest.VaccineProfileId, opt => opt.Ignore())
                .ForMember(dest => dest.ChildId, opt => opt.Ignore()) // Cannot change
                .ForMember(dest => dest.VaccineId, opt => opt.Ignore()) // Cannot change
                .ForMember(dest => dest.DoseNum, opt => opt.Ignore()) // Cannot change
                .ForMember(dest => dest.AppointmentId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()) // Cannot change
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore()) // Set in service
                .ForMember(dest => dest.Appointment, opt => opt.Ignore())
                .ForMember(dest => dest.Child, opt => opt.Ignore())
                .ForMember(dest => dest.Vaccine, opt => opt.Ignore());
        }
    }
}
