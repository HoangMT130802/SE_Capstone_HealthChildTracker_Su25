using AutoMapper;
using Contracts.DTOs.DailyRecord;
using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.MapperProfiles
{
    public class DailyRecordProfile : Profile
    {
        public DailyRecordProfile()
        {
            CreateMap<DailyRecord, DailyRecordDTO>()
                .ForMember(dest => dest.DailyRecordId, opt => opt.MapFrom(src => src.DailyRecordId))
                .ForMember(dest => dest.ChildId, opt => opt.MapFrom(src => src.ChildId))
                .ForMember(dest => dest.RecordDate, opt => opt.MapFrom(src => src.RecordDate))
                .ForMember(dest => dest.MilkAmount, opt => opt.MapFrom(src => src.MilkAmount))
                .ForMember(dest => dest.FeedingTimes, opt => opt.MapFrom(src => src.FeedingTimes))
                .ForMember(dest => dest.DiaperChanges, opt => opt.MapFrom(src => src.DiaperChanges))
                .ForMember(dest => dest.SleepHours, opt => opt.MapFrom(src => src.SleepHours))
                .ForMember(dest => dest.Note, opt => opt.MapFrom(src => src.Note))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt));

            CreateMap<CreateDailyRecordDTO, DailyRecord>()
           .ForMember(dest => dest.ChildId, opt => opt.MapFrom(src => src.ChildId))
           .ForMember(dest => dest.RecordDate, opt => opt.MapFrom(src => src.RecordDate))
           .ForMember(dest => dest.MilkAmount, opt => opt.MapFrom(src => src.MilkAmount))
           .ForMember(dest => dest.FeedingTimes, opt => opt.MapFrom(src => src.FeedingTimes))
           .ForMember(dest => dest.DiaperChanges, opt => opt.MapFrom(src => src.DiaperChanges))
           .ForMember(dest => dest.SleepHours, opt => opt.MapFrom(src => src.SleepHours))
           .ForMember(dest => dest.Note, opt => opt.MapFrom(src => src.Note))
           .ForMember(dest => dest.DailyRecordId, opt => opt.Ignore())
           .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()) // Set in service
           .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore()) // Set in service
           .ForMember(dest => dest.Child, opt => opt.Ignore());

            CreateMap<UpdateDailyRecordDTO, DailyRecord>()
                .ForMember(dest => dest.RecordDate, opt => opt.MapFrom(src => src.RecordDate))
                .ForMember(dest => dest.MilkAmount, opt => opt.MapFrom(src => src.MilkAmount))
                .ForMember(dest => dest.FeedingTimes, opt => opt.MapFrom(src => src.FeedingTimes))
                .ForMember(dest => dest.DiaperChanges, opt => opt.MapFrom(src => src.DiaperChanges))
                .ForMember(dest => dest.SleepHours, opt => opt.MapFrom(src => src.SleepHours))
                .ForMember(dest => dest.Note, opt => opt.MapFrom(src => src.Note))
                .ForMember(dest => dest.DailyRecordId, opt => opt.Ignore())
                .ForMember(dest => dest.ChildId, opt => opt.Ignore()) // Cannot change
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()) // Cannot change
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore()) // Set in service
                .ForMember(dest => dest.Child, opt => opt.Ignore());
        }
    }
}
