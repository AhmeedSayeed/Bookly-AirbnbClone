using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PL.Controllers
{
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly IWishlistService _wishlistService;

        public WishlistController(IWishlistService wishlistService)
        {
            _wishlistService = wishlistService;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(userIdClaim, out int userId);
            return userId;
        }

        // GET: /Wishlist/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var response = await _wishlistService.GetUserWishlistAsync(userId);

            return View(response.Data);
        }

        // POST: /Wishlist/Toggle  (called via AJAX from a listing card's heart icon)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int listingId)
        {
            var userId = GetCurrentUserId();
            var response = await _wishlistService.ToggleAsync(userId, listingId);

            // isWishlisted tells the front-end whether to render the heart as filled or empty
            return Json(new { success = response.Succeeded, isWishlisted = response.Data });
        }
    }
}