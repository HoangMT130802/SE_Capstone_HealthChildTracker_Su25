using AutoMapper;
using Contracts.DTOs.Child;
using Contracts.DTOs.GrowthRecord;
using Repositories.Entities;

namespace Contracts.MapperProfiles
{
    public class ChildProfile : Profile
    {
        public ChildProfile()
        {
            CreateMap<Child, ChildDTO>()
                .ForMember(dest => dest.ChildId, opt => opt.MapFrom(src => src.ChildId))
                .ForMember(dest => dest.MemberId, opt => opt.MapFrom(src => src.MemberId))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.BirthDate, opt => opt.MapFrom(src => src.BirthDate))
                .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender))
                .ForMember(dest => dest.BloodType, opt => opt.MapFrom(src => src.BloodType))
                .ForMember(dest => dest.imageURL, opt => opt.MapFrom(src => src.ImageUrl))
                .ForMember(dest => dest.AllergiesNotes, opt => opt.MapFrom(src => src.AllergiesNotes))
                .ForMember(dest => dest.MedicalHistory, opt => opt.MapFrom(src => src.MedicalHistory))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdateAt, opt => opt.MapFrom(src => src.UpdateAt));
            CreateMap<GrowthRecord, GrowthRecordDTO>()
            .ForMember(dest => dest.Note, opt => opt.MapFrom(src => src.Note));
            CreateMap<CreateChildDTO, Child>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.BirthDate, opt => opt.MapFrom(src => src.BirthDate))
                .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender))
                .ForMember(dest => dest.BloodType, opt => opt.MapFrom(src => src.BloodType))
                .ForMember(dest => dest.AllergiesNotes, opt => opt.MapFrom(src => src.AllergiesNotes))
                .ForMember(dest => dest.MedicalHistory, opt => opt.MapFrom(src => src.MedicalHistory))
                .ForMember(dest => dest.ChildId, opt => opt.Ignore())
                .ForMember(dest => dest.MemberId, opt => opt.Ignore()) 
                .ForMember(dest => dest.Status, opt => opt.Ignore()) 
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()) 
                .ForMember(dest => dest.UpdateAt, opt => opt.Ignore()) 
                .ForMember(dest => dest.Member, opt => opt.Ignore())
                .ForMember(dest => dest.ChildVaccineProfiles, opt => opt.Ignore())
                .ForMember(dest => dest.DailyRecords, opt => opt.Ignore())
                .ForMember(dest => dest.GrowthRecords, opt => opt.Ignore())
                .ForMember(dest => dest.VaccinationAppointments, opt => opt.Ignore());

            CreateMap<UpdateChildDTO, Child>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.BirthDate, opt => opt.MapFrom(src => src.BirthDate))
                .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender))
                .ForMember(dest => dest.BloodType, opt => opt.MapFrom(src => src.BloodType))
                .ForMember(dest => dest.AllergiesNotes, opt => opt.MapFrom(src => src.AllergiesNotes))
                .ForMember(dest => dest.MedicalHistory, opt => opt.MapFrom(src => src.MedicalHistory))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.ChildId, opt => opt.Ignore())
                .ForMember(dest => dest.MemberId, opt => opt.Ignore()) 
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()) 
                .ForMember(dest => dest.UpdateAt, opt => opt.Ignore())
                .ForMember(dest => dest.ImageUrl, opt => opt.Ignore())
                .ForMember(dest => dest.Member, opt => opt.Ignore())
                .ForMember(dest => dest.ChildVaccineProfiles, opt => opt.Ignore())
                .ForMember(dest => dest.DailyRecords, opt => opt.Ignore())
                .ForMember(dest => dest.GrowthRecords, opt => opt.Ignore())
                .ForMember(dest => dest.VaccinationAppointments, opt => opt.Ignore());
        }
    }
} 