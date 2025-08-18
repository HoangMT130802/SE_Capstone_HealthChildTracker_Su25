using AutoMapper;
using Contracts.DTOs.VaccinationFacilityPaymentAccount;
using Repositories.Entities;

namespace Contracts.MapperProfiles
{
    public class VaccinationFacilityPaymentAccountProfile : Profile
    {
        public VaccinationFacilityPaymentAccountProfile()
        {
            CreateMap<CreateVaccinationFacilityPaymentAccountDto, VaccinationFacilityPaymentAccount>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive ? "true" : "false"))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateOnly.FromDateTime(DateTime.UtcNow)))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateOnly.FromDateTime(DateTime.UtcNow)))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Facility, opt => opt.Ignore());

            CreateMap<UpdateVaccinationFacilityPaymentAccountDto, VaccinationFacilityPaymentAccount>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive ? "true" : "false"))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateOnly.FromDateTime(DateTime.UtcNow)))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.FacilityId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Facility, opt => opt.Ignore());

            CreateMap<VaccinationFacilityPaymentAccount, VaccinationFacilityPaymentAccountDto>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive == "true" || src.IsActive == "1"));
        }
    }
}
