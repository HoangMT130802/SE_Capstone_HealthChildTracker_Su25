using AutoMapper;
using Contracts.DTOs.Vaccine;
using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.MapperProfiles
{
    public class VaccineProfile : Profile
    {
        public VaccineProfile()
        {
            CreateMap<Vaccine, VaccineDTO>()
                .ForMember(dest => dest.VaccineId, opt => opt.MapFrom(src => src.VaccineId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Manufacturer, opt => opt.MapFrom(src => src.Manufacturer))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
                .ForMember(dest => dest.AgeGroup, opt => opt.MapFrom(src => src.AgeGroup))
                .ForMember(dest => dest.NumberOfDoses, opt => opt.MapFrom(src => src.NumberOfDoses))
                .ForMember(dest => dest.MinIntervalBetweenDoses, opt => opt.MapFrom(src => src.MinIntervalBetweenDoses))
                .ForMember(dest => dest.SideEffects, opt => opt.MapFrom(src => src.SideEffects))
                .ForMember(dest => dest.Contraindications, opt => opt.MapFrom(src => src.Contraindications))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
                .ForMember(dest => dest.DiseaseIds, opt => opt.MapFrom(src => src.VaccineDiseases.Select(vd => vd.DiseaseId).ToList()));

            CreateMap<CreateVaccineDTO, Vaccine>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Manufacturer, opt => opt.MapFrom(src => src.Manufacturer))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
                .ForMember(dest => dest.AgeGroup, opt => opt.MapFrom(src => src.AgeGroup))
                .ForMember(dest => dest.NumberOfDoses, opt => opt.MapFrom(src => src.NumberOfDoses))
                .ForMember(dest => dest.MinIntervalBetweenDoses, opt => opt.MapFrom(src => src.MinIntervalBetweenDoses))
                .ForMember(dest => dest.SideEffects, opt => opt.MapFrom(src => src.SideEffects))
                .ForMember(dest => dest.Contraindications, opt => opt.MapFrom(src => src.Contraindications))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.VaccineId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ChildVaccineProfiles, opt => opt.Ignore())
                .ForMember(dest => dest.FacilityVaccines, opt => opt.Ignore())
                .ForMember(dest => dest.OrderDetails, opt => opt.Ignore())
                .ForMember(dest => dest.PackageVaccines, opt => opt.Ignore())
                .ForMember(dest => dest.VaccinationAppointmentDetails, opt => opt.Ignore())
                .ForMember(dest => dest.VaccineDiseases, opt => opt.Ignore());
            CreateMap<UpdateVaccineDTO, Vaccine>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Manufacturer, opt => opt.MapFrom(src => src.Manufacturer))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
                .ForMember(dest => dest.AgeGroup, opt => opt.MapFrom(src => src.AgeGroup))
                .ForMember(dest => dest.NumberOfDoses, opt => opt.MapFrom(src => src.NumberOfDoses))
                .ForMember(dest => dest.MinIntervalBetweenDoses, opt => opt.MapFrom(src => src.MinIntervalBetweenDoses))
                .ForMember(dest => dest.SideEffects, opt => opt.MapFrom(src => src.SideEffects))
                .ForMember(dest => dest.Contraindications, opt => opt.MapFrom(src => src.Contraindications))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.VaccineId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ChildVaccineProfiles, opt => opt.Ignore())
                .ForMember(dest => dest.FacilityVaccines, opt => opt.Ignore())
                .ForMember(dest => dest.OrderDetails, opt => opt.Ignore())
                .ForMember(dest => dest.PackageVaccines, opt => opt.Ignore())
                .ForMember(dest => dest.VaccinationAppointmentDetails, opt => opt.Ignore())
                .ForMember(dest => dest.VaccineDiseases, opt => opt.MapFrom(src => src.DiseaseIds.Select(id => new VaccineDisease { DiseaseId = id }).ToList()));
        }
    }
}
