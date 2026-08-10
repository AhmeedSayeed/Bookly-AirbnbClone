using AutoMapper;
using BLL.ViewModels.Admin;
using BLL.ViewModels.Common;
using BLL.ViewModels.Listings;
using DAL.Models.Property;

namespace BLL.Mapping;

public class ListingProfile : Profile
{
    public ListingProfile()
    {
        CreateMap<Listing, ListingCardViewModel>()
            .ForMember(d => d.PrimaryPhotoUrl, o => o.MapFrom(s => s.Photos
                .OrderBy(p => p.DisplayOrder)
                .Select(p => p.Url)
                .FirstOrDefault()))
            .ForMember(d => d.AverageRating, o => o.MapFrom(s => s.Bookings
                .Where(b => b.Review != null)
                .Select(b => (double)b.Review!.Rating)
                .DefaultIfEmpty(0)
                .Average()))
            .ForMember(d => d.ReviewCount, o => o.MapFrom(s => s.Bookings.Count(b => b.Review != null)))
            .ForMember(d => d.IsWishlisted, o => o.Ignore());

        CreateMap<Listing, ListingSummaryViewModel>()
            .ForMember(d => d.PrimaryPhotoUrl, o => o.MapFrom(s => s.Photos
                .OrderBy(p => p.DisplayOrder)
                .Select(p => p.Url)
                .FirstOrDefault()))
            .ForMember(d => d.TotalBookings, o => o.MapFrom(s => s.Bookings.Count));

        CreateMap<Listing, ListingDetailsViewModel>()
            .ForMember(d => d.PropertyType, o => o.MapFrom(s => s.PropertyType.ToString()))
            .ForMember(d => d.CancellationPolicy, o => o.MapFrom(s => s.CancellationPolicy != null ? s.CancellationPolicy.ToString() : null))
            .ForMember(d => d.PhotoUrls, o => o.MapFrom(s => s.Photos.OrderBy(p => p.DisplayOrder).Select(p => p.Url)))
            .ForMember(d => d.Amenities, o => o.MapFrom(s => s.ListingAmenities.Select(la => la.Amenity.Name)))
            .ForMember(d => d.Host, o => o.MapFrom(s => s.Host))
            .ForMember(d => d.Reviews, o => o.MapFrom(s => s.Bookings
                .Where(b => b.Review != null)
                .Select(b => b.Review!)))
            .ForMember(d => d.Booking, o => o.Ignore());

        CreateMap<Listing, ListingAdminRowViewModel>()
            .ForMember(d => d.HostName, o => o.MapFrom(s => s.Host.FirstName + " " + s.Host.LastName));

        // Form -> Entity (create/edit)
        CreateMap<ListingFormViewModel, Listing>()
            .ForMember(d => d.Id, o => o.Condition(s => s.Id.HasValue))
            .ForMember(d => d.ListingAmenities, o => o.Ignore())
            .ForMember(d => d.Photos, o => o.Ignore());
    }
}