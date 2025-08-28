using AutoMapper;
using Contracts.DTOs.NotificationHistory;
using Repositories.Entities;

namespace Contracts.MapperProfiles;

public class NotificationHistoryProfile : Profile
{
    public NotificationHistoryProfile()
    {
        CreateMap<NotificationHistory, NotificationHistoryResponseDto>()
            .ForMember(dest => dest.ChildName, opt => opt.MapFrom(src => src.Child != null ? src.Child.FullName : null))
            .ForMember(dest => dest.VaccineName, opt => opt.MapFrom(src => src.Vaccine != null ? src.Vaccine.Name : null))
            .ForMember(dest => dest.DeliveryStatuses, opt => opt.MapFrom(src => src.NotificationDeliveryStatuses));

        CreateMap<NotificationDeliveryStatus, NotificationDeliveryStatusDto>()
            .ForMember(dest => dest.DeviceType, opt => opt.MapFrom(src => src.DeviceToken != null ? src.DeviceToken.DeviceType : null));
    }
}

