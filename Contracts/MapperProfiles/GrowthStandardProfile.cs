using AutoMapper;
using Contracts.DTOs.GrowthStandard;
using Repositories.Entities;

namespace Contracts.MapperProfiles
{
    public class GrowthStandardProfile : Profile
    {
        public GrowthStandardProfile()
        {
            CreateMap<GrowthStandard, GrowthStandardDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.AgeInMonths, opt => opt.MapFrom(src => src.AgeInMonths))
                .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender))
                .ForMember(dest => dest.Measurement, opt => opt.MapFrom(src => src.Measurement))
                .ForMember(dest => dest.Sd3neg, opt => opt.MapFrom(src => src.Sd3neg))
                .ForMember(dest => dest.Sd2neg, opt => opt.MapFrom(src => src.Sd2neg))
                .ForMember(dest => dest.Sd1neg, opt => opt.MapFrom(src => src.Sd1neg))
                .ForMember(dest => dest.Median, opt => opt.MapFrom(src => src.Median))
                .ForMember(dest => dest.Sd1pos, opt => opt.MapFrom(src => src.Sd1pos))
                .ForMember(dest => dest.Sd2pos, opt => opt.MapFrom(src => src.Sd2pos))
                .ForMember(dest => dest.Sd3pos, opt => opt.MapFrom(src => src.Sd3pos));
        }
    }
} 