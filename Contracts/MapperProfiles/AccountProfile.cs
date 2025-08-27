using AutoMapper;
using Contracts.DTOs.Account;
using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.MapperProfiles
{
    internal class AccountProfile:Profile
    {
        public AccountProfile() {
            CreateMap<UpdateAccountDTO, Account>()
              .ForMember(dest => dest.AccountId, opt => opt.Ignore())
              .ForMember(dest => dest.Password, opt => opt.Ignore())
              .ForMember(dest => dest.Role, opt => opt.Ignore())
              .ForMember(dest => dest.Status, opt => opt.Ignore())
              .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
              .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
              .ForMember(dest => dest.ImageUrl, opt => opt.Ignore())
              .ForMember(dest => dest.FacilityStaffs, opt => opt.Ignore())
              .ForMember(dest => dest.Members, opt => opt.Ignore())
              .ForMember(dest => dest.UserMemberships, opt => opt.Ignore());
            CreateMap<Account, AccountDTO>();
        }
        
    }
}
