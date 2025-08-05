using AutoMapper;
using Contracts.DTOs.VaccinationFacilityPaymentAccount;
using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.MapperProfiles
{
    public class VaccinationFacilityPaymentAccountProfile : Profile
    {
        public VaccinationFacilityPaymentAccountProfile()
        {
            CreateMap<CreateVaccinationFacilityPaymentAccountDto, VaccinationFacilityPaymentAccount>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateOnly.FromDateTime(DateTime.UtcNow)))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateOnly.FromDateTime(DateTime.UtcNow)))
                .ForMember(dest => dest.Facility, opt => opt.Ignore());

            CreateMap<UpdateVaccinationFacilityPaymentAccountDto, VaccinationFacilityPaymentAccount>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateOnly.FromDateTime(DateTime.UtcNow)))
                .ForMember(dest => dest.Facility, opt => opt.Ignore());

            CreateMap<VaccinationFacilityPaymentAccount, VaccinationFacilityPaymentAccountDto>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive == "true" || src.IsActive == "1"))
                .ForMember(dest => dest.FacilityId, opt => opt.MapFrom(src => src.FacilityId));
        }
    }
}
