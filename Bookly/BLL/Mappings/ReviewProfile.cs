using AutoMapper;
using BLL.ViewModels.Reviews;
using DAL.Models.Interactions;

namespace BLL.Mapping;

public class ReviewProfile : Profile
{
    public ReviewProfile()
    {
        CreateMap<Review, ReviewViewModel>()
            .ForMember(d => d.GuestName, o => o.MapFrom(s => s.Booking.Guest.FirstName + " " + s.Booking.Guest.LastName))
            .ForMember(d => d.GuestPhotoUrl, o => o.MapFrom(s => s.Booking.Guest.ProfilePhotoUrl))
            .ForMember(d => d.HostResponse, o => o.MapFrom(s => s.HostResponse != null ? s.HostResponse.Content : null));

        CreateMap<CreateReviewViewModel, Review>()
            .ForMember(d => d.CreatedAt, o => o.MapFrom(_ => DateTime.UtcNow));
    }
}
