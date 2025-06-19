using AutoMapper;
using Contracts.DTOs.GrowthRecord;
using Repositories.Entities;

namespace Contracts.MapperProfiles
{
    public class GrowthRecordProfile : Profile
    {
        public GrowthRecordProfile()
        {
            CreateMap<GrowthRecord, GrowthRecordDTO>()
                .ForMember(dest => dest.RecordId, opt => opt.MapFrom(src => src.RecordId))
                .ForMember(dest => dest.ChildId, opt => opt.MapFrom(src => src.ChildId))
                .ForMember(dest => dest.Height, opt => opt.MapFrom(src => src.Height))
                .ForMember(dest => dest.Weight, opt => opt.MapFrom(src => src.Weight))
                .ForMember(dest => dest.HeadCircumference, opt => opt.MapFrom(src => src.HeadCircumference))
                .ForMember(dest => dest.Bmi, opt => opt.MapFrom(src => src.Bmi))
                .ForMember(dest => dest.Note, opt => opt.MapFrom(src => src.Note))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
                .ForMember(dest => dest.ChildName, opt => opt.MapFrom(src => src.Child != null ? src.Child.FullName : ""))
                .ForMember(dest => dest.AgeInDays, opt => opt.MapFrom(src => 
                    src.Child != null ? 
                    (int)(src.CreatedAt - src.Child.BirthDate).TotalDays : 0));

            CreateMap<CreateGrowthRecordDTO, GrowthRecord>()
                .ForMember(dest => dest.ChildId, opt => opt.MapFrom(src => src.ChildId))
                .ForMember(dest => dest.Height, opt => opt.MapFrom(src => src.Height))
                .ForMember(dest => dest.Weight, opt => opt.MapFrom(src => src.Weight))
                .ForMember(dest => dest.HeadCircumference, opt => opt.MapFrom(src => src.HeadCircumference))
                .ForMember(dest => dest.Note, opt => opt.MapFrom(src => src.Note))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.RecordId, opt => opt.Ignore())
                .ForMember(dest => dest.Bmi, opt => opt.Ignore()) // Calculated in service
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore()) // Set in service
                .ForMember(dest => dest.Child, opt => opt.Ignore());

            CreateMap<UpdateGrowthRecordDTO, GrowthRecord>()
                .ForMember(dest => dest.Height, opt => opt.MapFrom(src => src.Height))
                .ForMember(dest => dest.Weight, opt => opt.MapFrom(src => src.Weight))
                .ForMember(dest => dest.HeadCircumference, opt => opt.MapFrom(src => src.HeadCircumference))
                .ForMember(dest => dest.Note, opt => opt.MapFrom(src => src.Note))
                .ForMember(dest => dest.RecordId, opt => opt.Ignore())
                .ForMember(dest => dest.ChildId, opt => opt.Ignore()) // Cannot change
                .ForMember(dest => dest.Bmi, opt => opt.Ignore()) // Calculated in service
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()) // Cannot change
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore()) // Set in service
                .ForMember(dest => dest.Child, opt => opt.Ignore());
        }
    }
} 