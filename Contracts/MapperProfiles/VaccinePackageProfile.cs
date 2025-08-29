using AutoMapper;
using Contracts.DTOs.ChildVaccineProfile;
using Contracts.DTOs.Order;
using Contracts.DTOs.VaccinePackage;
using Repositories.Entities;

namespace Contracts.MapperProfiles
{
    public class VaccinePackageProfile : Profile
    {
        public VaccinePackageProfile()
        {
            CreateMap<VaccinePackage, VaccinePackageDTO>()
                .ForMember(dest => dest.PackageVaccines, opt => opt.MapFrom(src => src.PackageVaccines));

            CreateMap<CreateVaccinePackageDTO, VaccinePackage>()
                .ForMember(dest => dest.PackageId, opt => opt.Ignore())
                .ForMember(dest => dest.Price, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Facility, opt => opt.Ignore())
                .ForMember(dest => dest.Orders, opt => opt.Ignore())
                .ForMember(dest => dest.PackageVaccines, opt => opt.Ignore());

            CreateMap<CreateVaccinePackageWithVaccinesDTO, VaccinePackage>()
                .ForMember(dest => dest.PackageId, opt => opt.Ignore())
                .ForMember(dest => dest.Price, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Facility, opt => opt.Ignore())
                .ForMember(dest => dest.Orders, opt => opt.Ignore())
                .ForMember(dest => dest.PackageVaccines, opt => opt.Ignore());

            CreateMap<UpdateVaccinePackageDTO, VaccinePackage>()
           .ForMember(dest => dest.PackageId, opt => opt.Ignore())
           .ForMember(dest => dest.Price, opt => opt.Ignore())
           .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
           .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
           .ForMember(dest => dest.Facility, opt => opt.Ignore())
           .ForMember(dest => dest.Orders, opt => opt.Ignore())
           .ForMember(dest => dest.PackageVaccines, opt => opt.Ignore());
            CreateMap<PackageVaccine, PackageVaccineDTO>()
            .ForMember(dest => dest.FacilityVaccineId, opt => opt.MapFrom(src => src.FacilityVaccineId))
            .ForMember(dest => dest.DiseaseId, opt => opt.MapFrom(src => src.DiseaseId))
            .ForMember(dest => dest.FacilityVaccine, opt => opt.MapFrom(src => src.FacilityVaccine))
            .ForMember(dest => dest.Disease, opt => opt.MapFrom(src => src.Disease));

            CreateMap<CreatePackageVaccineDTO, PackageVaccine>()
                .ForMember(dest => dest.FacilityVaccineId, opt => opt.MapFrom(src => src.FacilityVaccineId))
                .ForMember(dest => dest.PackageId, opt => opt.Ignore())
                .ForMember(dest => dest.PackageVaccineId, opt => opt.Ignore())
                .ForMember(dest => dest.DiseaseId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Package, opt => opt.Ignore())
                .ForMember(dest => dest.FacilityVaccine, opt => opt.Ignore());

            CreateMap<UpdatePackageVaccineDTO, PackageVaccine>()
                .ForMember(dest => dest.PackageId, opt => opt.Ignore())
                .ForMember(dest => dest.PackageVaccineId, opt => opt.Ignore())
                .ForMember(dest => dest.DiseaseId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Package, opt => opt.Ignore())
                .ForMember(dest => dest.FacilityVaccine, opt => opt.Ignore());
            CreateMap<AddPackageVaccineDTO, PackageVaccine>()
                .ForMember(dest => dest.FacilityVaccineId, opt => opt.MapFrom(src => src.FacilityVaccineId))
                .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity))
                .ForMember(dest => dest.PackageId, opt => opt.Ignore())
                .ForMember(dest => dest.PackageVaccineId, opt => opt.Ignore())
                .ForMember(dest => dest.DiseaseId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Package, opt => opt.Ignore())
                .ForMember(dest => dest.FacilityVaccine, opt => opt.Ignore());
            CreateMap<VaccineTemplate, VaccineRecordDTO>()
            .ForMember(dest => dest.DiseaseName, opt => opt.MapFrom(src => src.Disease.Name))
            .ForMember(dest => dest.RequiredDoseNum, opt => opt.MapFrom(src => src.DoseNum));
        }
    }
}