using BLL.DTOs;
using BLL.ViewModels.Wishlist;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BLL.Services.Interfaces
{
    public interface IWishlistService
    {
        Task<Response<WishlistViewModel>> GetUserWishlistAsync(int userId);

        Task<Response<bool>> ToggleAsync(int userId, int listingId);

        Task<bool> IsWishlistedAsync(int userId, int listingId);

        // Bulk lookup used by the search/browse grid, so we don't run one query per card
        Task<HashSet<int>> GetWishlistedListingIdsAsync(int userId);
    }
}