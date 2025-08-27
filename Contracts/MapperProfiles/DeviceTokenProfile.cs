using AutoMapper;
using Contracts.DTOs.DeviceToken;
using Repositories.Entities;

namespace Contracts.MapperProfiles
{
    public class DeviceTokenProfile : Profile
    {
        public DeviceTokenProfile()
        {
            // Entity → DTO
            CreateMap<DeviceToken, DeviceTokenResponseDto>()
                .ForMember(dest => dest.DeviceTokenId, opt => opt.MapFrom(src => src.DeviceTokenId))
                .ForMember(dest => dest.Token, opt => opt.MapFrom(src => src.Token))
                .ForMember(dest => dest.DeviceType, opt => opt.MapFrom(src => src.DeviceType))
                .ForMember(dest => dest.DeviceInfo, opt => opt.MapFrom(src => src.DeviceInfo))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.LastUsedAt, opt => opt.MapFrom(src => src.LastUsedAt));

            // CreateDTO → Entity
            CreateMap<DeviceTokenCreateDto, DeviceToken>()
                .ForMember(dest => dest.DeviceTokenId, opt => opt.Ignore())
                .ForMember(dest => dest.AccountId, opt => opt.Ignore()) // Sẽ set trong service
                .ForMember(dest => dest.Token, opt => opt.MapFrom(src => src.Token))
                .ForMember(dest => dest.DeviceType, opt => opt.MapFrom(src => src.DeviceType))
                .ForMember(dest => dest.DeviceInfo, opt => opt.MapFrom(src => src.DeviceInfo))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()) // Sẽ set trong service
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore()) // Sẽ set trong service
                .ForMember(dest => dest.LastUsedAt, opt => opt.Ignore()) // Sẽ set trong service
                .ForMember(dest => dest.Account, opt => opt.Ignore()); // Navigation property
        }
    }
}
