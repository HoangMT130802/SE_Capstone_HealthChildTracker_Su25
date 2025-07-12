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
            // ✅ Entity → DTO: Convert long timestamp to DateTime
            CreateMap<ChildVaccineProfile, ChildVaccineProfileDTO>()
                .ForMember(dest => dest.VaccineProfileId, opt => opt.MapFrom(src => src.VaccineProfileId))
                .ForMember(dest => dest.ChildId, opt => opt.MapFrom(src => src.ChildId))
                .ForMember(dest => dest.DiseaseId, opt => opt.MapFrom(src => src.DiseaseId))
                .ForMember(dest => dest.AppointmentId, opt => opt.MapFrom(src => src.AppointmentId)) // nullable
                .ForMember(dest => dest.VaccineId, opt => opt.MapFrom(src => src.VaccineId))
                .ForMember(dest => dest.DoseNum, opt => opt.MapFrom(src => src.DoseNum))
                .ForMember(dest => dest.ExpectedDate, opt => opt.MapFrom(src => src.ExpectedDate))
                .ForMember(dest => dest.ActualDate, opt => opt.MapFrom(src => src.ActualDate))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.IsRequired, opt => opt.MapFrom(src => src.IsRequired))
                .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority))
                .ForMember(dest => dest.Note, opt => opt.MapFrom(src => src.Note))
                // ✅ Convert long timestamp to DateTime
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTimeOffset.FromUnixTimeSeconds(src.CreatedAt).DateTime))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTimeOffset.FromUnixTimeSeconds(src.UpdatedAt).DateTime));

            // ✅ CreateDTO → Entity: Convert DateTime to long timestamp
            CreateMap<CreateChildVaccineProfileDTO, ChildVaccineProfile>()
                .ForMember(dest => dest.ChildId, opt => opt.MapFrom(src => src.ChildId))
                .ForMember(dest => dest.DiseaseId, opt => opt.MapFrom(src => src.DiseaseId))
                .ForMember(dest => dest.AppointmentId, opt => opt.MapFrom(src => src.AppointmentId)) // nullable - có thể tạo vaccine profile trước khi có appointment
                .ForMember(dest => dest.VaccineId, opt => opt.MapFrom(src => src.VaccineId))
                .ForMember(dest => dest.DoseNum, opt => opt.MapFrom(src => src.DoseNum))
                .ForMember(dest => dest.ExpectedDate, opt => opt.MapFrom(src => src.ExpectedDate))
                .ForMember(dest => dest.ActualDate, opt => opt.MapFrom(src => src.ActualDate))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.IsRequired, opt => opt.MapFrom(src => src.IsRequired))
                .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority))
                .ForMember(dest => dest.Note, opt => opt.MapFrom(src => src.Note))
                .ForMember(dest => dest.VaccineProfileId, opt => opt.Ignore())
                // ✅ Timestamps will be set in service
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()) 
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                // ✅ Navigation properties
                .ForMember(dest => dest.Appointment, opt => opt.Ignore())
                .ForMember(dest => dest.Child, opt => opt.Ignore())
                .ForMember(dest => dest.Disease, opt => opt.Ignore())
                .ForMember(dest => dest.Vaccine, opt => opt.Ignore());

            // ✅ UpdateDTO → Entity: Handle nullable fields
            CreateMap<UpdateChildVaccineProfileDTO, ChildVaccineProfile>()
                .ForMember(dest => dest.DiseaseId, opt => opt.MapFrom(src => src.DiseaseId ?? 0))
                .ForMember(dest => dest.ExpectedDate, opt => opt.MapFrom(src => src.ExpectedDate))
                .ForMember(dest => dest.ActualDate, opt => opt.MapFrom(src => src.ActualDate))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.IsRequired, opt => opt.MapFrom(src => src.IsRequired))
                .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority))
                .ForMember(dest => dest.Note, opt => opt.MapFrom(src => src.Note))
                // ✅ Fields that cannot be changed
                .ForMember(dest => dest.VaccineProfileId, opt => opt.Ignore())
                .ForMember(dest => dest.ChildId, opt => opt.Ignore())
                .ForMember(dest => dest.VaccineId, opt => opt.Ignore())
                .ForMember(dest => dest.DoseNum, opt => opt.Ignore())
                .ForMember(dest => dest.AppointmentId, opt => opt.Ignore()) // Managed separately
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore()) // Set in service
                // ✅ Navigation properties
                .ForMember(dest => dest.Appointment, opt => opt.Ignore())
                .ForMember(dest => dest.Child, opt => opt.Ignore())
                .ForMember(dest => dest.Disease, opt => opt.Ignore())
                .ForMember(dest => dest.Vaccine, opt => opt.Ignore());
        }
    }
}
