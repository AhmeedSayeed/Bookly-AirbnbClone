using AutoMapper;
using BLL.ViewModels.Notifications;
using DAL.Models.Interactions;

namespace BLL.Mapping;

public class NotificationProfile : Profile
{
    public NotificationProfile()
    {
        CreateMap<Notification, NotificationViewModel>();
    }
}
