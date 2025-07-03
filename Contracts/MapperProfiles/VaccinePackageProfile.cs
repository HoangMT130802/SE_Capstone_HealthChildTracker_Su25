using AutoMapper;
using Contracts.DTOs.VaccinePackage;
using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                .ForMember(dest => dest.Price, opt => opt.Ignore()) // Bỏ qua Price
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Facility, opt => opt.Ignore())
                .ForMember(dest => dest.Orders, opt => opt.Ignore())
                .ForMember(dest => dest.PackageVaccines, opt => opt.Ignore());

            CreateMap<CreateVaccinePackageWithVaccinesDTO, VaccinePackage>()
                .ForMember(dest => dest.PackageId, opt => opt.Ignore())
                .ForMember(dest => dest.Price, opt => opt.Ignore()) // Bỏ qua Price
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Facility, opt => opt.Ignore())
                .ForMember(dest => dest.Orders, opt => opt.Ignore())
                .ForMember(dest => dest.PackageVaccines, opt => opt.Ignore());

            CreateMap<UpdateVaccinePackageDTO, VaccinePackage>()
                .ForMember(dest => dest.PackageId, opt => opt.Ignore())
                .ForMember(dest => dest.Price, opt => opt.Ignore()) // Bỏ qua Price
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Facility, opt => opt.Ignore())
                .ForMember(dest => dest.Orders, opt => opt.Ignore())
                .ForMember(dest => dest.PackageVaccines, opt => opt.Ignore());

            CreateMap<PackageVaccine, PackageVaccineDTO>()
                .ForMember(dest => dest.VaccineId, opt => opt.MapFrom(src => src.FacilityVaccineId));

            CreateMap<CreatePackageVaccineDTO, PackageVaccine>()
                .ForMember(dest => dest.FacilityVaccineId, opt => opt.MapFrom(src => src.VaccineId))
                .ForMember(dest => dest.PackageId, opt => opt.Ignore())
                .ForMember(dest => dest.PackageVaccineId, opt => opt.Ignore())
                .ForMember(dest => dest.DiseaseId, opt => opt.Ignore()) // Bỏ qua DiseaseId
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Package, opt => opt.Ignore())
                .ForMember(dest => dest.FacilityVaccine, opt => opt.Ignore());

            CreateMap<UpdatePackageVaccineDTO, PackageVaccine>()
                .ForMember(dest => dest.FacilityVaccineId, opt => opt.MapFrom(src => src.VaccineId))
                .ForMember(dest => dest.PackageId, opt => opt.Ignore())
                .ForMember(dest => dest.PackageVaccineId, opt => opt.Ignore())
                .ForMember(dest => dest.DiseaseId, opt => opt.Ignore()) // Bỏ qua DiseaseId
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Package, opt => opt.Ignore())
                .ForMember(dest => dest.FacilityVaccine, opt => opt.Ignore());
        }
    }
}
