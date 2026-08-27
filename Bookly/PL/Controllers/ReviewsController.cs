using BLL.Services.Interfaces;
using BLL.ViewModels.Reviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PL.Controllers
{
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly IReviewService _reviewService;

        public ReviewsController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(userIdClaim, out int userId);
            return userId;
        }

        // GET: /Reviews/Create/{bookingId}
        [HttpGet]
        public async Task<IActionResult> Create(int bookingId)
        {
            var guestId = GetCurrentUserId();
            var response = await _reviewService.GetReviewFormAsync(bookingId, guestId);

            if (!response.Succeeded)
            {
                TempData["ErrorMessage"] = response.Message;
                return RedirectToAction("MyTrips", "Bookings");
            }

            return View(response.Data);
        }

        // POST: /Reviews/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateReviewViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var guestId = GetCurrentUserId();
            var response = await _reviewService.SubmitReviewAsync(guestId, model);

            if (!response.Succeeded)
            {
                ModelState.AddModelError(string.Empty, response.Message ?? "Failed to submit review.");
                return View(model);
            }

            TempData["SuccessMessage"] = response.Message;
            return RedirectToAction("MyTrips", "Bookings");
        }

        // GET: /Reviews/Respond/{reviewId}
        [HttpGet]
        public async Task<IActionResult> Respond(int reviewId)
        {
            var hostId = GetCurrentUserId();
            var response = await _reviewService.GetRespondFormAsync(reviewId, hostId);

            if (!response.Succeeded)
            {
                TempData["ErrorMessage"] = response.Message;
                return RedirectToAction("MyListings", "Listings");
            }

            return View(response.Data);
        }

        // POST: /Reviews/Respond
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Respond(HostResponseViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var hostId = GetCurrentUserId();
            var response = await _reviewService.RespondToReviewAsync(hostId, model);

            if (!response.Succeeded)
            {
                ModelState.AddModelError(string.Empty, response.Message ?? "Failed to post response.");
                return View(model);
            }

            TempData["SuccessMessage"] = response.Message;
            return RedirectToAction("MyListings", "Listings");
        }
    }
}