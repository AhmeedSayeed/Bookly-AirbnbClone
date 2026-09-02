using AutoMapper;
using BLL.ViewModels.Notifications;
using DAL.Models.Interactions;

namespace BLL.Mapping;

public class NotificationProfile : Profile
{
    public NotificationProfile()
    {
        CreateMap<Notification, NotificationViewModel>()
            .ForMember(dest => dest.LegacyMessage,
                opt => opt.MapFrom(src => src.Message));
    }
}