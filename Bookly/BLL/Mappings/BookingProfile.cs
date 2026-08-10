using AutoMapper;
using DAL.Models;
using DAL.Enums;
using BLL.ViewModels.Bookings;
using DAL.Models.Reservations;

namespace BLL.Mapping;

public class BookingProfile : Profile
{
    public BookingProfile()
    {
        CreateMap<Booking, BookingCardViewModel>()
            .ForMember(d => d.ListingTitle, o => o.MapFrom(s => s.Listing.Title))
            .ForMember(d => d.ListingCity, o => o.MapFrom(s => s.Listing.City))
            .ForMember(d => d.ListingPhotoUrl, o => o.MapFrom(s => s.Listing.Photos
                .OrderBy(p => p.DisplayOrder).Select(p => p.Url).FirstOrDefault()))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.CanReview, o => o.MapFrom(s => s.Status == BookingStatus.Completed && s.Review == null));

        CreateMap<Booking, BookingRequestCardViewModel>()
            .ForMember(d => d.ListingTitle, o => o.MapFrom(s => s.Listing.Title))
            .ForMember(d => d.GuestName, o => o.MapFrom(s => s.Guest.FirstName + " " + s.Guest.LastName))
            .ForMember(d => d.GuestPhotoUrl, o => o.MapFrom(s => s.Guest.ProfilePhotoUrl))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));

        CreateMap<Booking, BookingSummaryViewModel>()
            .ForMember(d => d.ListingTitle, o => o.MapFrom(s => s.Listing.Title));

        CreateMap<Booking, BookingConfirmationViewModel>()
            .ForMember(d => d.BookingId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.ListingTitle, o => o.MapFrom(s => s.Listing.Title))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));

        CreateMap<Booking, BookingDetailsViewModel>()
            .ForMember(d => d.ListingTitle, o => o.MapFrom(s => s.Listing.Title))
            .ForMember(d => d.ListingAddress, o => o.MapFrom(s => s.Listing.Address))
            .ForMember(d => d.ListingPhotoUrl, o => o.MapFrom(s => s.Listing.Photos
                .OrderBy(p => p.DisplayOrder).Select(p => p.Url).FirstOrDefault()))
            .ForMember(d => d.Guest, o => o.MapFrom(s => s.Guest))
            .ForMember(d => d.Host, o => o.MapFrom(s => s.Listing.Host))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));

        // Input -> Entity
        CreateMap<BookingRequestViewModel, Booking>()
            .ForMember(d => d.GuestId, o => o.Ignore())
            .ForMember(d => d.TotalPrice, o => o.Ignore())
            .ForMember(d => d.Status, o => o.MapFrom(_ => BookingStatus.Pending))
            .ForMember(d => d.CreatedAt, o => o.MapFrom(_ => DateTime.UtcNow));
    }
}
