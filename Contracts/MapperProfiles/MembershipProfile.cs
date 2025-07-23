using AutoMapper;
using Contracts.DTOs.Membership;
using Repositories.Entities;

namespace Contracts.MapperProfiles
{
    public class MembershipProfile : Profile
    {
        public MembershipProfile()
        {
            // Entity to DTO
            CreateMap<Membership, MembershipDTO>();

            // Create DTO to Entity
            CreateMap<CreateMembershipDTO, Membership>()
                .ForMember(dest => dest.MembershipId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UserMemberships, opt => opt.Ignore());

            // Update DTO to Entity
            CreateMap<UpdateMembershipDTO, Membership>()
                .ForMember(dest => dest.MembershipId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UserMemberships, opt => opt.Ignore())
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
} 