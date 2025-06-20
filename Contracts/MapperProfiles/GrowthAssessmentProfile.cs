using AutoMapper;
using Contracts.DTOs.GrowthAssessment;
using Contracts.DTOs.GrowthRecord;
using Repositories.Entities;

namespace Contracts.MapperProfiles
{
    public class GrowthAssessmentProfile : Profile
    {
        public GrowthAssessmentProfile()
        {
            // Map từ GrowthRecordDTO sang GrowthRecord entity để sử dụng trong assessment
            CreateMap<GrowthRecordDTO, GrowthRecord>()
                .ForMember(dest => dest.RecordId, opt => opt.MapFrom(src => src.RecordId))
                .ForMember(dest => dest.ChildId, opt => opt.MapFrom(src => src.ChildId))
                .ForMember(dest => dest.Height, opt => opt.MapFrom(src => src.Height))
                .ForMember(dest => dest.Weight, opt => opt.MapFrom(src => src.Weight))
                .ForMember(dest => dest.Bmi, opt => opt.MapFrom(src => src.Bmi))
                .ForMember(dest => dest.HeadCircumference, opt => opt.MapFrom(src => src.HeadCircumference))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
                .ForMember(dest => dest.Note, opt => opt.MapFrom(src => src.Note))
                .ForMember(dest => dest.Child, opt => opt.Ignore()); // Sẽ được load riêng
        }
    }
} 