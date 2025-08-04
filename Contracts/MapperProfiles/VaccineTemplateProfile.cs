using AutoMapper;
using Contracts.DTOs.VaccineTemplate;
using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.MapperProfiles
{
    public class VaccineTemplateProfile : Profile
    {
        public VaccineTemplateProfile()
        {
            CreateMap<VaccineTemplate, VaccineTemplateDTO>()
                .ForMember(dest => dest.DiseaseName, opt => opt.MapFrom(src => src.Disease != null ? src.Disease.Name : null));

            CreateMap<CreateVaccineTemplateDTO, VaccineTemplate>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Disease, opt => opt.Ignore());

            CreateMap<UpdateVaccineTemplateDTO, VaccineTemplate>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Disease, opt => opt.Ignore());
        }
    }
}
