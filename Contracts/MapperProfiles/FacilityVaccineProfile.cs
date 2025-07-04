using AutoMapper;
using Contracts.DTOs.FacilityVaccine;
using Contracts.DTOs.Vaccine;
using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.MapperProfiles
{
    public class FacilityVaccineProfile:Profile
    {
        public FacilityVaccineProfile()
        {
            CreateMap<CreateFacilityVaccineDTO, FacilityVaccine>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.FacilityVaccineId, opt => opt.Ignore())
                .ForMember(dest => dest.Facility, opt => opt.Ignore())
                .ForMember(dest => dest.Vaccine, opt => opt.Ignore());

            // Ánh xạ từ UpdateFacilityVaccineDTO sang FacilityVaccine
            CreateMap<UpdateFacilityVaccineDTO, FacilityVaccine>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.FacilityVaccineId, opt => opt.Ignore())
                .ForMember(dest => dest.Facility, opt => opt.Ignore())
                .ForMember(dest => dest.Vaccine, opt => opt.Ignore());

            // Ánh xạ từ FacilityVaccine sang FacilityVaccineDTO
            CreateMap<FacilityVaccine, FacilityVaccineDTO>()
                .ForMember(dest => dest.Vaccine, opt => opt.MapFrom(src => src.Vaccine));

            // Ánh xạ từ Vaccine sang VaccineDTO
            CreateMap<Vaccine, VaccineDTO>();
        }
}
}
