using AutoMapper;
using BLL.DTOs;
using BLL.DTOs.Listing;
using BLL.Services.Interfaces;
using BLL.ViewModels.Listings;
using BLL.ViewModels.Availability;
using DAL.Enums;
using DAL.Models.Common;
using DAL.Models.Property;
using DAL.Repository.Interfaces;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Services.Implementation
{
    public class ListingService : IListingService
    {
        private readonly IRepository<Listing> _listingRepo;
        private readonly IFileUploader _fileUploader;
        private readonly IMapper _mapper;

        public ListingService(IRepository<Listing> listingRepo, IFileUploader fileUploader, IMapper mapper)
        {
            _listingRepo = listingRepo;
            _fileUploader = fileUploader;
            _mapper = mapper;
        }

        public async Task<Response<PagedResult<ListingCardDto>>> SearchListingsAsync(ListingSearchRequestDto request)
        {
            var predicate = PredicateBuilder.New<Listing>(true);

            predicate = predicate.And(l => l.IsActive);

            if (!string.IsNullOrWhiteSpace(request.City))
                predicate = predicate.And(l => l.City.Contains(request.City));

            if (request.Guests.HasValue)
                predicate = predicate.And(l => l.MaxGuests >= request.Guests.Value);

            if (request.MinPrice.HasValue)
                predicate = predicate.And(l => l.PricePerNight >= request.MinPrice.Value);

            if (request.MaxPrice.HasValue)
                predicate = predicate.And(l => l.PricePerNight <= request.MaxPrice.Value);

            if (request.AmenityIds != null && request.AmenityIds.Any())
            {
                foreach (var amenityId in request.AmenityIds)
                {
                    predicate = predicate.And(l => l.ListingAmenities.Any(la => la.AmenityId == amenityId));
                }
            }

            if (request.CheckIn.HasValue && request.CheckOut.HasValue)
            {
                var checkIn = request.CheckIn.Value;
                var checkOut = request.CheckOut.Value;

                predicate = predicate.And(l => !l.Bookings.Any(b =>
                    b.Status == BookingStatus.Confirmed &&
                    b.CheckInDate < checkOut &&
                    b.CheckOutDate > checkIn));

                predicate = predicate.And(l => !l.BlockedDates.Any(bd => bd.Date >= checkIn && bd.Date < checkOut));
            }

            var pagedListings = await _listingRepo.GetAllPaginatedEnhancedAsync<ListingCardDto>(
                selector: l => new ListingCardDto
                {
                    Id = l.Id,
                    Title = l.Title,
                    City = l.City,
                    PricePerNight = l.PricePerNight,
                    ThumbnailUrl = l.Photos.OrderBy(p => p.DisplayOrder).Select(p => p.Url).FirstOrDefault(),
                    HostName = l.Host.FirstName,
                    Latitude = l.Latitude,
                    Longitude = l.Longitude
                },
                pageNumber: request.PageNumber,
                pageSize: request.PageSize,
                filter: predicate,
                expandable: true,
                orderBy: q => q.OrderByDescending(l => l.CreatedAt),
                include: q => q.Include(l => l.Photos).Include(l => l.Host)
            );

            return Response<PagedResult<ListingCardDto>>.Success(pagedListings);
        }

        public async Task<Response<int>> CreateAsync(ListingFormViewModel model, int hostId)
        {
            var listing = _mapper.Map<Listing>(model);
            listing.HostId = hostId;
            listing.IsActive = true;

            listing.ListingAmenities ??= new List<ListingAmenity>();
            listing.Photos ??= new List<ListingPhoto>();

            if (model.SelectedAmenityIds != null && model.SelectedAmenityIds.Any())
            {
                foreach (var amenityId in model.SelectedAmenityIds)
                {
                    listing.ListingAmenities.Add(new ListingAmenity { AmenityId = amenityId });
                }
            }

            if (model.NewPhotos != null && model.NewPhotos.Any())
            {
                int displayOrder = 1;

                foreach (var photo in model.NewPhotos)
                {
                    var uploadResponse = await _fileUploader.SaveFileAsync(photo, "listings", true);

                    if (!uploadResponse.Succeeded)
                    {
                        return Response<int>.FailWithKey(
                            ResponseStatus.Error,
                            "FailedToUploadPhoto",
                            uploadResponse.Message);
                    }

                    listing.Photos.Add(new ListingPhoto
                    {
                        Url = uploadResponse.Data,
                        DisplayOrder = displayOrder++
                    });
                }
            }

            await _listingRepo.AddAsync(listing);
            var saved = await _listingRepo.SaveAsync();

            if (saved > 0)
                return Response<int>.SuccessWithKey(
                    listing.Id,
                    "ListingCreatedSuccessfully");

            return Response<int>.FailWithKey(
                ResponseStatus.Error,
                "FailedToCreateListing");
        }

        public async Task<Response<ListingDetailsViewModel>> GetDetailsAsync(int id)
        {
            var query = _listingRepo.GetAllAsIQueryable()
                .Include(l => l.Host)
                .Include(l => l.Photos)
                .Include(l => l.ListingAmenities)
                    .ThenInclude(la => la.Amenity)
<<<<<<< HEAD
               .Include(l => l.Bookings)
                  .ThenInclude(b => b.Review)
                    .ThenInclude(r => r.HostResponse);
=======
                .Include(l => l.Bookings)
                    .ThenInclude(b => b.Review)
                        .ThenInclude(r => r.HostResponse);
>>>>>>> 1d78647728deb23e0845e21ee93619e4bfb182db

            var listing = await query.FirstOrDefaultAsync(l => l.Id == id);

            if (listing == null)
                return Response<ListingDetailsViewModel>.FailWithKey(
                    ResponseStatus.NotFound,
                    "ListingNotFound");

            var viewModel = _mapper.Map<ListingDetailsViewModel>(listing);

            return Response<ListingDetailsViewModel>.Success(viewModel);
        }

        public async Task<Response<bool>> UpdateAsync(ListingFormViewModel model, int currentUserId)
        {
            var existingListing = await _listingRepo.GetAllAsIQueryable()
                .Include(l => l.ListingAmenities)
                .Include(l => l.Photos)
                .FirstOrDefaultAsync(l => l.Id == model.Id);

            if (existingListing == null)
                return Response<bool>.FailWithKey(
                    ResponseStatus.NotFound,
                    "ListingNotFound");

            if (existingListing.HostId != currentUserId)
                return Response<bool>.FailWithKey(
                    ResponseStatus.Forbidden,
                    "CannotEditListing");

            _mapper.Map(model, existingListing);

            existingListing.ListingAmenities.Clear();

            if (model.SelectedAmenityIds != null && model.SelectedAmenityIds.Any())
            {
                foreach (var amenityId in model.SelectedAmenityIds)
                {
                    existingListing.ListingAmenities.Add(new ListingAmenity
                    {
                        ListingId = existingListing.Id,
                        AmenityId = amenityId
                    });
                }
            }

            if (model.DeletedPhotoIds != null && model.DeletedPhotoIds.Any())
            {
                var photosToRemove = existingListing.Photos
                    .Where(p => model.DeletedPhotoIds.Contains(p.Id))
                    .ToList();

                foreach (var photo in photosToRemove)
                {
                    existingListing.Photos.Remove(photo);
                }
            }

            if (model.NewPhotos != null && model.NewPhotos.Any())
            {
                int displayOrder = existingListing.Photos.Any()
                    ? existingListing.Photos.Max(p => p.DisplayOrder) + 1
                    : 1;

                foreach (var photo in model.NewPhotos)
                {
                    var uploadResponse = await _fileUploader.SaveFileAsync(photo, "listings", true);

                    if (!uploadResponse.Succeeded)
                    {
                        return Response<bool>.FailWithKey(
                            ResponseStatus.Error,
                            "FailedToUploadPhoto",
                            uploadResponse.Message);
                    }

                    existingListing.Photos.Add(new ListingPhoto
                    {
                        Url = uploadResponse.Data,
                        DisplayOrder = displayOrder++
                    });
                }
            }

            _listingRepo.Update(existingListing);
            var saved = await _listingRepo.SaveAsync();

            return saved > 0
                ? Response<bool>.SuccessWithKey(true, "ListingUpdated")
                : Response<bool>.FailWithKey(
                    ResponseStatus.Error,
                    "NoChangesWereSaved");
        }

        public async Task<Response<bool>> DeleteAsync(int id, int currentUserId)
        {
            var listing = await _listingRepo.GetByIdAsync(id);

            if (listing == null)
                return Response<bool>.FailWithKey(
                    ResponseStatus.NotFound,
                    "ListingNotFound");

            if (listing.HostId != currentUserId)
                return Response<bool>.FailWithKey(
                    ResponseStatus.Forbidden,
                    "CannotDeleteListing");

            _listingRepo.Delete(id);
            var saved = await _listingRepo.SaveAsync();

            return saved > 0
                ? Response<bool>.SuccessWithKey(true, "ListingDeleted")
                : Response<bool>.FailWithKey(
                    ResponseStatus.Error,
                    "FailedToDeleteListing");
        }

        public async Task<Response<List<ListingSummaryViewModel>>> GetListingsByHostIdAsync(int hostId)
        {
            var query = _listingRepo.GetAllAsIQueryable()
                .Include(l => l.Photos)
                .Include(l => l.Bookings)
                .Where(l => l.HostId == hostId)
                .OrderByDescending(l => l.CreatedAt);

            var listings = await query.ToListAsync();
            var viewModels = _mapper.Map<List<ListingSummaryViewModel>>(listings);

            return Response<List<ListingSummaryViewModel>>.Success(viewModels);
        }

        public async Task<Response<ListingFormViewModel>> GetListingForEditAsync(int id)
        {
            var listing = await _listingRepo.GetAllAsIQueryable()
                .Include(l => l.ListingAmenities)
                .Include(l => l.Photos)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (listing == null)
                return Response<ListingFormViewModel>.FailWithKey(
                    ResponseStatus.NotFound,
                    "ListingNotFound");

            var viewModel = _mapper.Map<ListingFormViewModel>(listing);

            viewModel.SelectedAmenityIds = listing.ListingAmenities
                .Select(la => la.AmenityId)
                .ToList();

            return Response<ListingFormViewModel>.Success(viewModel);
        }

        public async Task<Response<AvailabilityCalendarViewModel>> GetAvailabilityCalendarAsync(
            int listingId,
            int currentUserId)
        {
            // Get it with its reservations and blocked dates
            var listing = await _listingRepo.GetAllAsIQueryable()
                .Include(l => l.BlockedDates)
                .Include(l => l.Bookings)
                .FirstOrDefaultAsync(l => l.Id == listingId);

            // Check property existance
            if (listing == null)
                return Response<AvailabilityCalendarViewModel>.FailWithKey(
                    ResponseStatus.NotFound,
                    "ListingNotFound");

            // check that current user is property owner
            if (listing.HostId != currentUserId)
                return Response<AvailabilityCalendarViewModel>.FailWithKey(
                    ResponseStatus.Forbidden,
                    "CannotManageListing");

            // extract all reserved days (pending or confirmed)
            var bookedDates = new List<DateTime>();
            var activeBookings = listing.Bookings
                .Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Pending)
                .ToList();

            foreach (var booking in activeBookings)
            {
                for (var date = booking.CheckInDate.Date;
                     date < booking.CheckOutDate.Date;
                     date = date.AddDays(1))
                {
                    if (!bookedDates.Contains(date))
                        bookedDates.Add(date);
                }
            }

            // Get blocked dates
            var blockedDates = listing.BlockedDates
                .Select(bd => bd.Date.Date)
                .OrderBy(d => d)
                .ToList();

            // prepare ViewModel and return it
            var viewModel = new AvailabilityCalendarViewModel
            {
                ListingId = listing.Id,
                ListingTitle = listing.Title,
                BookedDates = bookedDates,
                BlockedDates = blockedDates
            };

            return Response<AvailabilityCalendarViewModel>.Success(viewModel);
        }

        public async Task<Response<bool>> UpdateAvailabilityCalendarAsync(
            AvailabilityCalendarViewModel model,
            int currentUserId)
        {
            var listing = await _listingRepo.GetAllAsIQueryable()
                .Include(l => l.BlockedDates)
                .FirstOrDefaultAsync(l => l.Id == model.ListingId);

            if (listing == null)
                return Response<bool>.FailWithKey(
                    ResponseStatus.NotFound,
                    "ListingNotFound");

            if (listing.HostId != currentUserId)
                return Response<bool>.FailWithKey(
                    ResponseStatus.Forbidden,
                    "CannotManageListing");

            // delete blocked dates to retype them
            var today = DateTime.UtcNow.Date;

            var existingFutureBlocks = listing.BlockedDates
                .Where(bd => bd.Date.Date >= today)
                .ToList();

            foreach (var oldBlock in existingFutureBlocks)
            {
                listing.BlockedDates.Remove(oldBlock);
            }

            // add new date that user choosed (validation: it can't be from pasts or repeated)
            if (model.BlockedDates != null && model.BlockedDates.Any())
            {
                var validDates = model.BlockedDates
                    .Where(d => d.Date >= today)
                    .Distinct();

                foreach (var date in validDates)
                {
                    listing.BlockedDates.Add(new BlockedDate
                    {
                        ListingId = listing.Id,
                        Date = date.Date,
                        Reason = "Blocked by host"
                    });
                }
            }

            // save changes in DB
            var saved = await _listingRepo.SaveAsync();

            return Response<bool>.SuccessWithKey(
                true,
                "AvailabilityCalendarUpdatedSuccessfully");
        }
    }
}