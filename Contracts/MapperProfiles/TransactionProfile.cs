using AutoMapper;
using Contracts.DTOs.Transaction;
using Contracts.DTOs.UserMembership;
using Contracts.DTOs.FacilitySubcription;
using Repositories.Entities;

namespace Contracts.MapperProfiles
{
    public class TransactionProfile : Profile
    {
        public TransactionProfile()
        {
            // Transaction mappings
            CreateMap<Transaction, TransactionDTO>()
                .ForMember(dest => dest.UserMembership, opt => opt.MapFrom(src => src.UserMembership))
                .ForMember(dest => dest.FacilityMembershipSubscription, opt => opt.MapFrom(src => src.FacilityMembershipSubscription));
            
            CreateMap<CreateTransactionDTO, Transaction>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateOnly.FromDateTime(DateTime.UtcNow)));
            
            // FacilityMembershipSubscription mappings
            CreateMap<FacilityMembershipSubscription, FacilityMembershipSubscriptionDTO>()
                .ForMember(dest => dest.FacilityName, opt => opt.MapFrom(src => src.Facility.FacilityName))
                .ForMember(dest => dest.FacilityMembershipName, opt => opt.MapFrom(src => src.FacilityMembership.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.FacilityMembership.Description))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.FacilityMembership.Price));
        }
    }
} 