using BLL.DTOs;
using BLL.DTOs.Listing;
using BLL.ViewModels.Availability;
using BLL.ViewModels.Listings;
using DAL.Models.Common;

namespace BLL.Services.Interfaces
{
    public interface IListingService
    {
        public Task<Response<PagedResult<ListingCardDto>>> SearchListingsAsync(ListingSearchRequestDto request);
        public Task<Response<int>> CreateAsync(ListingFormViewModel model, int hostId);
        public Task<Response<ListingDetailsViewModel>> GetDetailsAsync(int id);
        public Task<Response<bool>> UpdateAsync(ListingFormViewModel model, int currentUserId);
        public Task<Response<bool>> DeleteAsync(int id, int currentUserId);
        public Task<Response<List<ListingSummaryViewModel>>> GetListingsByHostIdAsync(int hostId);
        public Task<Response<ListingFormViewModel>> GetListingForEditAsync(int id);
        public Task<Response<AvailabilityCalendarViewModel>> GetAvailabilityCalendarAsync(int listingId, int currentUserId);
        public Task<Response<bool>> UpdateAvailabilityCalendarAsync(AvailabilityCalendarViewModel model, int currentUserId);
    }
}