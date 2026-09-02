using BLL.Interfaces;
using BLL.Services.Interfaces;
using BLL.ViewModels.Home;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PL.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHomeService _homeService;
        private readonly IWishlistService _wishlistService;

        public HomeController(IHomeService homeService, IWishlistService wishlistService)
        {
            _homeService = homeService;
            _wishlistService = wishlistService;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(userIdClaim, out int userId);
            return userId;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var response = await _homeService.GetHomeDataAsync();

            if (!response.Succeeded)
            {
                return View(new HomeViewModel());
            }

            // Mark which of the featured listings the current user has already wishlisted
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var currentUserId = GetCurrentUserId();
                var wishlistedIds = await _wishlistService.GetWishlistedListingIdsAsync(currentUserId);

                foreach (var listing in response.Data.FeaturedListings)
                {
                    listing.IsWishlisted = wishlistedIds.Contains(listing.Id);
                }
            }

            return View(response.Data);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}