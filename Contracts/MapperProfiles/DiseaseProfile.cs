using AutoMapper;
using Contracts.DTOs.Disease;
using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.MapperProfiles
{
    public class DiseaseProfile : Profile
    {
        public DiseaseProfile()
        {
            CreateMap<Disease, DiseaseDTO>()
                .ForMember(dest => dest.DiseaseId, opt => opt.MapFrom(src => src.DiseaseId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Symptoms, opt => opt.MapFrom(src => src.Symptoms))
                .ForMember(dest => dest.Treatment, opt => opt.MapFrom(src => src.Treatment))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt));
            CreateMap<CreateDiseaseDTO, Disease>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Symptoms, opt => opt.MapFrom(src => src.Symptoms))
                .ForMember(dest => dest.Treatment, opt => opt.MapFrom(src => src.Treatment))
                .ForMember(dest => dest.DiseaseId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ChildVaccineProfiles, opt => opt.Ignore())
                .ForMember(dest => dest.VaccineDiseases, opt => opt.Ignore())
                .ForMember(dest => dest.VaccineTemplates, opt => opt.Ignore());

            CreateMap<UpdateDiseaseDTO, Disease>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Symptoms, opt => opt.MapFrom(src => src.Symptoms))
                .ForMember(dest => dest.Treatment, opt => opt.MapFrom(src => src.Treatment))
                .ForMember(dest => dest.DiseaseId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ChildVaccineProfiles, opt => opt.Ignore())
                .ForMember(dest => dest.VaccineDiseases, opt => opt.Ignore())
                .ForMember(dest => dest.VaccineTemplates, opt => opt.Ignore());
        }
    }
}
