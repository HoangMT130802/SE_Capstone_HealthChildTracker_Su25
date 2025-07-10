using AutoMapper;
using Contracts.DTOs.Disease;
using Contracts.DTOs.FacilityVaccine;
using Contracts.DTOs.Order;
using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.MapperProfiles
{
    public class OrderProfile : Profile
    {
        public OrderProfile()
        {
            CreateMap<CreatePackageOrderDTO, Order>()
                .ForMember(dest => dest.OrderId, opt => opt.Ignore())
                .ForMember(dest => dest.MemberId, opt => opt.Ignore())
                .ForMember(dest => dest.TotalAmount, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Member, opt => opt.Ignore())
               .ForMember(dest => dest.Package, opt => opt.Ignore())
                .ForMember(dest => dest.OrderDetails, opt => opt.Ignore())
                .ForMember(dest => dest.VaccinationAppointments, opt => opt.Ignore());

            CreateMap<SelectedVaccineDTO, OrderDetail>()
                .ForMember(dest => dest.OrderDetailId, opt => opt.Ignore())
                .ForMember(dest => dest.OrderId, opt => opt.Ignore())
                .ForMember(dest => dest.FacilityVaccineId, opt => opt.MapFrom(src => src.FacilityVaccineId))
                .ForMember(dest => dest.DiseaseId, opt => opt.MapFrom(src => src.DiseaseId))
                .ForMember(dest => dest.RemainingQuantity, opt => opt.MapFrom(src => src.Quantity))
                .ForMember(dest => dest.Price, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Disease, opt => opt.Ignore())
                .ForMember(dest => dest.FacilityVaccine, opt => opt.Ignore())
                .ForMember(dest => dest.FacilityVaccineNavigation, opt => opt.Ignore())
                .ForMember(dest => dest.Order, opt => opt.Ignore());

            CreateMap<Order, OrderDTO>()
                .ForMember(dest => dest.OrderDetails, opt => opt.MapFrom(src => src.OrderDetails));
            CreateMap<OrderDetail, OrderDetailDTO>();
            CreateMap<FacilityVaccine, FacilityVaccineDTO>();
            CreateMap<Disease, DiseaseDTO>();
        }
    }
}
