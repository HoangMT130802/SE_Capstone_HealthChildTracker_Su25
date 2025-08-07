using AutoMapper;
using Contracts.DTOs.FacilityRating;
using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.MapperProfiles
{
    public class FacilityRatingMapperProfile : Profile
    {
        public FacilityRatingMapperProfile()
        {
            CreateMap<FacilityRating, FacilityRatingDTO>()
                .ForMember(dest => dest.FacilityId, opt => opt.MapFrom(src => src.FacilityId))
                .ForMember(dest => dest.MemberId, opt => opt.MapFrom(src => src.MemberId))
                .ReverseMap();

            CreateMap<CreateFacilityRatingDTO, FacilityRating>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Rating, opt => opt.Ignore()); 

            CreateMap<UpdateFacilityRatingDTO, FacilityRating>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Rating, opt => opt.Ignore()); 
        }
    }
}
