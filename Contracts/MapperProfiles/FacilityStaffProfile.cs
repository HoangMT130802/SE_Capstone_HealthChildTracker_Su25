using AutoMapper;
using Contracts.DTOs.FacilityStaff;
using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.MapperProfiles
{
    public class FacilityStaffProfile : Profile
    {
        public FacilityStaffProfile()
        {
            CreateMap<FacilityStaff, FacilityStaffDTO>()
                .ReverseMap();
            CreateMap<UpdateFacilityStaffDTO, FacilityStaff>().ForMember(dest => dest.StaffId, opt => opt.Ignore()).ForMember(dest => dest.AccountId, opt => opt.Ignore()).ForMember(dest => dest.FacilityId, opt => opt.Ignore()).ForMember(dest => dest.CreatedAt, opt => opt.Ignore()).ForMember(dest => dest.UpdatedAt, opt => opt.Ignore()).ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status ?? false));
        }
    }
}
