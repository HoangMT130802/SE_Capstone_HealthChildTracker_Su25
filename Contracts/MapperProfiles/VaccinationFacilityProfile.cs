using AutoMapper;
using Contracts.DTOs.VaccinationFacility;
using Repositories.Entities;

namespace Contracts.MapperProfiles
{
    public class VaccinationFacilityProfile : Profile
    {
        public VaccinationFacilityProfile()
        {
            // Entity to DTO mappings
            CreateMap<VaccinationFacility, VaccinationFacilityDTO>()
                .ForMember(dest => dest.FacilityId, opt => opt.MapFrom(src => src.FacilityId))
                .ForMember(dest => dest.FacilityName, opt => opt.MapFrom(src => src.FacilityName))
                .ForMember(dest => dest.LicenseNumber, opt => opt.MapFrom(src => src.LicenseNumber))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt));

            // DTO to Entity mappings
            CreateMap<CreateVaccinationFacilityDTO, VaccinationFacility>()
                .ForMember(dest => dest.FacilityName, opt => opt.MapFrom(src => src.FacilityName))
                .ForMember(dest => dest.LicenseNumber, opt => opt.MapFrom(src => src.LicenseNumber))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => 1)) // Active by default
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTimeOffset.UtcNow.ToUnixTimeSeconds()))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTimeOffset.UtcNow.ToUnixTimeSeconds()))
                .ForMember(dest => dest.FacilityId, opt => opt.Ignore())
                .ForMember(dest => dest.AppointmentSchedules, opt => opt.Ignore())
                .ForMember(dest => dest.FacilityMembershipSubscriptions, opt => opt.Ignore())
                .ForMember(dest => dest.FacilityRatings, opt => opt.Ignore())
                .ForMember(dest => dest.FacilityStaffs, opt => opt.Ignore())
                .ForMember(dest => dest.FacilityVaccines, opt => opt.Ignore())
                .ForMember(dest => dest.VaccinePackages, opt => opt.Ignore());

            CreateMap<UpdateVaccinationFacilityDTO, VaccinationFacility>()
                .ForMember(dest => dest.FacilityId, opt => opt.MapFrom(src => src.FacilityId))
                .ForMember(dest => dest.FacilityName, opt => opt.MapFrom(src => src.FacilityName))
                .ForMember(dest => dest.LicenseNumber, opt => opt.MapFrom(src => src.LicenseNumber))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTimeOffset.UtcNow.ToUnixTimeSeconds()))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.AppointmentSchedules, opt => opt.Ignore())
                .ForMember(dest => dest.FacilityMembershipSubscriptions, opt => opt.Ignore())
                .ForMember(dest => dest.FacilityRatings, opt => opt.Ignore())
                .ForMember(dest => dest.FacilityStaffs, opt => opt.Ignore())
                .ForMember(dest => dest.FacilityVaccines, opt => opt.Ignore())
                .ForMember(dest => dest.VaccinePackages, opt => opt.Ignore());
        }
    }
} 