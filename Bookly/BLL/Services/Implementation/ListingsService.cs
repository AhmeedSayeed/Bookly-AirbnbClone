using AutoMapper;
using BLL.DTOs;
using BLL.DTOs.Elasticsearch;
using BLL.DTOs.Listing;
using BLL.Services.Interfaces;
using BLL.ViewModels.Availability;
using BLL.ViewModels.Common;
using BLL.ViewModels.Listings;
using DAL.Enums;
using DAL.Models.Common;
using DAL.Models.Property;
using DAL.Repository.Interfaces;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Services.Implementation
{
    public class ListingService : IListingService
    {
        private readonly IRepository<Listing> _listingRepo;
        private readonly IFileUploader _fileUploader;
        private readonly IElasticListingService _elasticService;
        private readonly IMapper _mapper;

        public ListingService(IRepository<Listing> listingRepo, IFileUploader fileUploader, IElasticListingService elasticService, IMapper mapper)
        {
            _listingRepo = listingRepo;
            _fileUploader = fileUploader;
            _elasticService = elasticService;
            _mapper = mapper;
        }

        public async Task<Response<PagedResult<ListingCardViewModel>>> SearchListingsAsync(ListingSearchRequestDto request)
        {
            var pagedListings = await _elasticService.SearchAsync(request);

            var listingIds = pagedListings.Items.Select(i => i.Id).ToList();

            var listingsData = await _listingRepo.GetAllAsIQueryable()
                .Where(l => listingIds.Contains(l.Id))
                .Include(l => l.Bookings)
                    .ThenInclude(b => b.Review)
                .AsSplitQuery()
                .ToListAsync();

            var viewModels = new List<ListingCardViewModel>();

            foreach (var item in pagedListings.Items)
            {
                var dbListing = listingsData.FirstOrDefault(l => l.Id == item.Id);

                double avgRating = 0;
                int reviewCount = 0;

                if (dbListing != null)
                {
                    var reviews = dbListing.Bookings
                        .Where(b => b.Review != null)
                        .Select(b => b.Review)
                        .ToList();

                    if (reviews.Any())
                    {
                        avgRating = reviews.Average(r => r.Rating);
                        reviewCount = reviews.Count;
                    }
                }

                viewModels.Add(new ListingCardViewModel
                {
                    Id = item.Id,
                    Title = item.Title,
                    City = item.City,
                    Country = "",
                    PricePerNight = item.PricePerNight,
                    PrimaryPhotoUrl = item.ThumbnailUrl,
                    AverageRating = avgRating,
                    ReviewCount = reviewCount,
                    IsWishlisted = false
                });
            }

            var result = new PagedResult<ListingCardViewModel>
            {
                Items = viewModels,
                TotalCount = pagedListings.TotalCount,
                PageIndex = pagedListings.PageIndex,
                PageSize = pagedListings.PageSize
            };

            return Response<PagedResult<ListingCardViewModel>>.Success(result);
        }

        public async Task<(decimal MinPrice, decimal MaxPrice)> GetGlobalMinMaxPriceAsync()
        {
            var listings = await _listingRepo.GetAllAsIQueryable()
                .Where(l => l.IsActive)
                .ToListAsync();

            var hasListings = listings.Any();

            if (!hasListings)
            {
                return (0, 10000);
            }

            var minPrice = listings.Min(l => l.PricePerNight);
            var maxPrice = listings.Max(l => l.PricePerNight);

            if (minPrice == maxPrice)
            {
                maxPrice = minPrice + 500;
            }

            return (minPrice, maxPrice);
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
            {
                await SyncListingToElasticsearchAsync(listing.Id);
                return Response<int>.SuccessWithKey(listing.Id, "ListingCreatedSuccessfully");
            }

            return Response<int>.FailWithKey(ResponseStatus.Error, "FailedToCreateListing");
        }

        public async Task<Response<ListingDetailsViewModel>> GetDetailsAsync(int id)
        {
            var query = _listingRepo.GetAllAsIQueryable()
                .Include(l => l.Host)
                .Include(l => l.Photos)
                .Include(l => l.ListingAmenities)
                    .ThenInclude(la => la.Amenity)
                .Include(l => l.BlockedDates)
                .Include(l => l.Bookings)
                    .ThenInclude(b => b.Review)
                      .ThenInclude(r => r.HostResponse)
                .AsSplitQuery();

            var listing = await query.FirstOrDefaultAsync(l => l.Id == id);

            if (listing == null)
                return Response<ListingDetailsViewModel>.FailWithKey(ResponseStatus.NotFound, "ListingNotFound");

            var viewModel = _mapper.Map<ListingDetailsViewModel>(listing);

            var unavailable = new List<string>();
            var today = DateTime.UtcNow.Date;

            if (listing.BlockedDates != null)
            {
                unavailable.AddRange(listing.BlockedDates
                    .Where(bd => bd.Date.Date >= today)
                    .Select(bd => bd.Date.ToString("yyyy-MM-dd")));
            }

            var activeBookingsForDates = await _listingRepo.GetAllAsIQueryable()
                .Where(l => l.Id == id)
                .SelectMany(l => l.Bookings)
                .Where(b => b.Status != BookingStatus.Cancelled && b.Status != BookingStatus.Rejected)
                .Select(b => new { b.CheckInDate, b.CheckOutDate })
                .ToListAsync();

            foreach (var b in activeBookingsForDates)
            {
                for (var d = b.CheckInDate.Date; d <= b.CheckOutDate.Date; d = d.AddDays(1))
                {
                    var str = d.ToString("yyyy-MM-dd");
                    if (!unavailable.Contains(str))
                        unavailable.Add(str);
                }
            }

            viewModel.UnavailableDates = unavailable;
            return Response<ListingDetailsViewModel>.Success(viewModel);
        }

        public async Task<Response<bool>> UpdateAsync(ListingFormViewModel model, int currentUserId)
        {
            var existingListing = await _listingRepo.GetAllAsIQueryable()
                .Include(l => l.ListingAmenities)
                .Include(l => l.Photos)
                .FirstOrDefaultAsync(l => l.Id == model.Id);

            if (existingListing == null)
                return Response<bool>.FailWithKey(ResponseStatus.NotFound, "ListingNotFound");

            if (existingListing.HostId != currentUserId)
                return Response<bool>.FailWithKey(ResponseStatus.Forbidden, "CannotEditListing");

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
                        return Response<bool>.FailWithKey(ResponseStatus.Error, "FailedToUploadPhoto", uploadResponse.Message);
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

            if (saved > 0)
            {
                await SyncListingToElasticsearchAsync(existingListing.Id);
                return Response<bool>.SuccessWithKey(true, "ListingUpdated");
            }

            return Response<bool>.FailWithKey(ResponseStatus.Error, "NoChangesWereSaved");
        }

        public async Task<Response<bool>> DeleteAsync(int id, int currentUserId)
        {
            var listing = await _listingRepo.GetByIdAsync(id);

            if (listing == null)
                return Response<bool>.FailWithKey(ResponseStatus.NotFound, "ListingNotFound");

            if (listing.HostId != currentUserId)
                return Response<bool>.FailWithKey(ResponseStatus.Forbidden, "CannotDeleteListing");

            _listingRepo.Delete(id);
            var saved = await _listingRepo.SaveAsync();

            if (saved > 0)
            {
                await _elasticService.DeleteListingAsync(id);
                return Response<bool>.SuccessWithKey(true, "ListingDeleted");
            }

            return Response<bool>.FailWithKey(ResponseStatus.Error, "FailedToDeleteListing");
        }

        public async Task<Response<List<ListingSummaryViewModel>>> GetListingsByHostIdAsync(int hostId)
        {
            var query = _listingRepo.GetAllAsIQueryable()
                .Include(l => l.Photos)
                .Include(l => l.Bookings)
                .Where(l => l.HostId == hostId)
                .OrderByDescending(l => l.CreatedAt)
                .AsSplitQuery();

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
                return Response<ListingFormViewModel>.FailWithKey(ResponseStatus.NotFound, "ListingNotFound");

            var viewModel = _mapper.Map<ListingFormViewModel>(listing);

            viewModel.SelectedAmenityIds = listing.ListingAmenities
                .Select(la => la.AmenityId)
                .ToList();

            return Response<ListingFormViewModel>.Success(viewModel);
        }

        public async Task<Response<AvailabilityCalendarViewModel>> GetAvailabilityCalendarAsync(int listingId, int currentUserId)
        {
            var listing = await _listingRepo.GetAllAsIQueryable()
                .Include(l => l.BlockedDates)
                .FirstOrDefaultAsync(l => l.Id == listingId);

            if (listing == null)
                return Response<AvailabilityCalendarViewModel>.FailWithKey(ResponseStatus.NotFound, "ListingNotFound");

            if (listing.HostId != currentUserId)
                return Response<AvailabilityCalendarViewModel>.FailWithKey(ResponseStatus.Forbidden, "CannotManageListing");

            var bookedDates = new List<DateTime>();

            var activeBookingsForDates = await _listingRepo.GetAllAsIQueryable()
                .Where(l => l.Id == listingId)
                .SelectMany(l => l.Bookings)
                .Where(b => b.Status != BookingStatus.Cancelled && b.Status != BookingStatus.Rejected)
                .Select(b => new { b.CheckInDate, b.CheckOutDate })
                .ToListAsync();

            foreach (var b in activeBookingsForDates)
            {
                for (var date = b.CheckInDate.Date; date <= b.CheckOutDate.Date; date = date.AddDays(1))
                {
                    if (!bookedDates.Contains(date))
                        bookedDates.Add(date);
                }
            }

            var blockedDates = listing.BlockedDates
                .Select(bd => bd.Date.Date)
                .OrderBy(d => d)
                .ToList();

            var viewModel = new AvailabilityCalendarViewModel
            {
                ListingId = listing.Id,
                ListingTitle = listing.Title,
                BookedDates = bookedDates,
                BlockedDates = blockedDates
            };

            return Response<AvailabilityCalendarViewModel>.Success(viewModel);
        }

        public async Task<Response<bool>> UpdateAvailabilityCalendarAsync(AvailabilityCalendarViewModel model, int currentUserId)
        {
            var listing = await _listingRepo.GetAllAsIQueryable()
                .Include(l => l.BlockedDates)
                .FirstOrDefaultAsync(l => l.Id == model.ListingId);

            if (listing == null)
                return Response<bool>.FailWithKey(ResponseStatus.NotFound, "ListingNotFound");

            if (listing.HostId != currentUserId)
                return Response<bool>.FailWithKey(ResponseStatus.Forbidden, "CannotManageListing");

            var today = DateTime.UtcNow.Date;

            var existingFutureBlocks = listing.BlockedDates
                .Where(bd => bd.Date.Date >= today)
                .ToList();

            foreach (var oldBlock in existingFutureBlocks)
            {
                listing.BlockedDates.Remove(oldBlock);
            }

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

            var saved = await _listingRepo.SaveAsync();

            if (saved > 0)
            {
                await SyncListingToElasticsearchAsync(listing.Id);
            }

            return Response<bool>.SuccessWithKey(true, "AvailabilityCalendarUpdatedSuccessfully");
        }

        private async Task SyncListingToElasticsearchAsync(int listingId)
        {
            var listing = await _listingRepo.GetAllAsIQueryable()
                .Include(l => l.Host)
                .Include(l => l.Photos)
                .Include(l => l.ListingAmenities)
                .Include(l => l.BlockedDates)
                .FirstOrDefaultAsync(l => l.Id == listingId);

            if (listing == null) return;

            var document = new ListingDocument
            {
                Id = listing.Id,
                Title = listing.Title,
                City = listing.City,
                Description = listing.Description,
                PricePerNight = listing.PricePerNight,
                ThumbnailUrl = listing.Photos.OrderBy(p => p.DisplayOrder).Select(p => p.Url).FirstOrDefault(),
                HostName = listing.Host?.FirstName ?? "",
                Latitude = listing.Latitude,
                Longitude = listing.Longitude,
                MaxGuests = listing.MaxGuests,
                Bedrooms = listing.Bedrooms,
                Bathrooms = listing.Bathrooms,
                PropertyType = listing.PropertyType.ToString() ?? "",
                CancellationPolicy = listing.CancellationPolicy?.ToString() ?? "",
                AmenityIds = listing.ListingAmenities.Select(la => la.AmenityId).ToList(),
                CreatedAt = listing.CreatedAt
            };

            var unavailable = new HashSet<DateTime>();
            var today = DateTime.UtcNow.Date;

            if (listing.BlockedDates != null)
            {
                foreach (var bd in listing.BlockedDates.Where(bd => bd.Date.Date >= today))
                {
                    unavailable.Add(bd.Date.Date);
                }
            }

            var activeBookingsForDates = await _listingRepo.GetAllAsIQueryable()
                .Where(l => l.Id == listingId)
                .SelectMany(l => l.Bookings)
                .Where(b => b.Status != BookingStatus.Cancelled && b.Status != BookingStatus.Rejected)
                .Select(b => new { b.CheckInDate, b.CheckOutDate })
                .ToListAsync();

            foreach (var b in activeBookingsForDates)
            {
                for (var d = b.CheckInDate.Date; d <= b.CheckOutDate.Date; d = d.AddDays(1))
                {
                    unavailable.Add(d);
                }
            }

            document.UnavailableDates = unavailable.ToList();

            await _elasticService.IndexListingAsync(document);
        }
    }
}