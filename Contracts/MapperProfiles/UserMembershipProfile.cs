using AutoMapper;
using Contracts.DTOs.UserMembership;
using Repositories.Entities;

namespace Contracts.MapperProfiles
{
    public class UserMembershipProfile : Profile
    {
        public UserMembershipProfile()
        {
            // Entity to DTO với navigation properties
            CreateMap<UserMembership, UserMembershipDTO>()
                .ForMember(dest => dest.AccountName, opt => opt.MapFrom(src => src.Account.AccountName))
                .ForMember(dest => dest.MembershipName, opt => opt.MapFrom(src => src.Membership.Name))
                .ForMember(dest => dest.MembershipDescription, opt => opt.MapFrom(src => src.Membership.Description))
                .ForMember(dest => dest.MembershipPrice, opt => opt.MapFrom(src => src.Membership.Price))
                .ForMember(dest => dest.MembershipBenefits, opt => opt.MapFrom(src => src.Membership.Benefits));

            // SubscribeMembershipDTO không cần map trực tiếp vì nó chỉ là input DTO
        }
    }
} 