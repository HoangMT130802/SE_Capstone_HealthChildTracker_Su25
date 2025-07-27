using AutoMapper;
using Contracts.DTOs.Vaccine;
using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.MapperProfiles
{
    public class VaccineProfile : Profile
    {
        public VaccineProfile()
        {
            // ✅ Entity → DTO
            CreateMap<Vaccine, VaccineDTO>()
                .ForMember(dest => dest.Diseases, opt => opt.MapFrom(src =>
                    src.VaccineDiseases != null
                    ? src.VaccineDiseases.Select(vd => vd.Disease).ToList()
                    : null));

            // ✅ DTO → Entity
            CreateMap<VaccineDTO, Vaccine>()
                .ForMember(dest => dest.VaccineId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ChildVaccineProfiles, opt => opt.Ignore())
                .ForMember(dest => dest.FacilityVaccines, opt => opt.Ignore())
                .ForMember(dest => dest.VaccinationAppointmentDetails, opt => opt.Ignore())
                .ForMember(dest => dest.VaccineDiseases, opt => opt.Ignore());
            CreateMap<CreateVaccineDTO, Vaccine>()
            .ForMember(dest => dest.VaccineId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.VaccineDiseases, opt => opt.Ignore())
            .ForMember(dest => dest.ChildVaccineProfiles, opt => opt.Ignore())
            .ForMember(dest => dest.FacilityVaccines, opt => opt.Ignore())
            .ForMember(dest => dest.VaccinationAppointmentDetails, opt => opt.Ignore());
            CreateMap<UpdateVaccineDTO, Vaccine>()
                .ForMember(dest => dest.VaccineId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ChildVaccineProfiles, opt => opt.Ignore())
                .ForMember(dest => dest.FacilityVaccines, opt => opt.Ignore())
                .ForMember(dest => dest.VaccinationAppointmentDetails, opt => opt.Ignore())
                .ForMember(dest => dest.VaccineDiseases, opt => opt.Ignore());
        }
    }
}
