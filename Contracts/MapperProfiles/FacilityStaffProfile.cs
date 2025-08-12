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

        }
    }
}
