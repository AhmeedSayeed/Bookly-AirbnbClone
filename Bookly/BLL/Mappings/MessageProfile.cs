using AutoMapper;
using BLL.ViewModels.Messages;
using DAL.Models.Interactions;

namespace BLL.Mapping;

public class MessageProfile : Profile
{
    public MessageProfile()
    {
        CreateMap<Message, MessageViewModel>()
            .ForMember(d => d.SenderName, o => o.MapFrom(s => s.Sender.FirstName + " " + s.Sender.LastName))
            .ForMember(d => d.IsMine, o => o.Ignore());

        CreateMap<MessageFormViewModel, Message>()
            .ForMember(d => d.SenderId, o => o.Ignore())
            .ForMember(d => d.SentAt, o => o.MapFrom(_ => DateTime.UtcNow))
            .ForMember(d => d.IsRead, o => o.MapFrom(_ => false));
    }
}
