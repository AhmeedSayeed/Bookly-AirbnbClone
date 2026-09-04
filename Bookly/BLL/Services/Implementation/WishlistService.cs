using BLL.DTOs;
using BLL.Services.Interfaces;
using BLL.ViewModels.Common;
using BLL.ViewModels.Wishlist;
using DAL.Models.Interactions;
using DAL.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Services.Implementation
{
    public class WishlistService : IWishlistService
    {
        private readonly IRepository<Wishlist> _wishlistRepo;

        public WishlistService(
            IRepository<Wishlist> wishlistRepo)
        {
            _wishlistRepo = wishlistRepo;
        }

        public async Task<Response<WishlistViewModel>> GetUserWishlistAsync(int userId)
        {
            var listings = await _wishlistRepo.GetAllAsIQueryable()
                .Where(w => w.UserId == userId)
                .Select(w => new ListingCardViewModel
                {
                    Id = w.Listing.Id,
                    Title = w.Listing.Title,
                    City = w.Listing.City,
                    Country = w.Listing.Country,
                    PricePerNight = w.Listing.PricePerNight,
                    PrimaryPhotoUrl = w.Listing.Photos
                        .OrderBy(p => p.DisplayOrder)
                        .Select(p => p.Url)
                        .FirstOrDefault(),
                    AverageRating = w.Listing.Bookings
                        .Where(b => b.Review != null)
                        .Select(b => (double?)b.Review!.Rating)
                        .Average() ?? 0,
                    ReviewCount = w.Listing.Bookings.Count(b => b.Review != null),
                    IsWishlisted = true
                })
                .ToListAsync();

            var viewModel = new WishlistViewModel
            {
                Listings = listings
            };

            return Response<WishlistViewModel>.Success(viewModel);
        }

        public async Task<Response<bool>> ToggleAsync(int userId, int listingId)
        {
            var existingEntry = await _wishlistRepo.GetAllAsIQueryable()
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ListingId == listingId);

            if (existingEntry != null)
            {
                _wishlistRepo.Delete(existingEntry.Id);
                await _wishlistRepo.SaveAsync();
                return Response<bool>.SuccessWithKey(false, "RemovedFromWishlist");
            }

            var newEntry = new Wishlist
            {
                UserId = userId,
                ListingId = listingId
            };

            await _wishlistRepo.AddAsync(newEntry);
            await _wishlistRepo.SaveAsync();

            return Response<bool>.SuccessWithKey(true, "AddedToWishlist");
        }

        public async Task<bool> IsWishlistedAsync(int userId, int listingId)
        {
            return await _wishlistRepo.GetAllAsIQueryable()
                .AnyAsync(w => w.UserId == userId && w.ListingId == listingId);
        }

        public async Task<HashSet<int>> GetWishlistedListingIdsAsync(int userId)
        {
            var ids = await _wishlistRepo.GetAllAsIQueryable()
                .Where(w => w.UserId == userId)
                .Select(w => w.ListingId)
                .ToListAsync();

            return ids.ToHashSet();
        }
    }
}