using BLL.Services.Interfaces;
using BLL.ViewModels.Bookings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;

namespace PL.Controllers
{
    [Authorize]
    public class BookingsController : Controller
    {
        private readonly IBookingService _bookingService;

        public BookingsController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(userIdClaim, out int userId);
            return userId;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestBooking(BookingRequestViewModel request)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fill in all booking details correctly.";
                return RedirectToAction("Details", "Listings", new { id = request.ListingId });
            }

            int userId = GetCurrentUserId();
            var response = await _bookingService.RequestBookingAsync(userId, request);

            if (!response.Succeeded)
            {
                TempData["BookingError"] = response.Message;
                return RedirectToAction("Details", "Listings", new { id = request.ListingId });
            }

            TempData["SuccessMessage"] = response.Message;
            return RedirectToAction(nameof(Confirmation), new { id = response.Data });
        }

        [HttpGet]
        public async Task<IActionResult> Confirmation(int id)
        {
            int userId = GetCurrentUserId();
            var response = await _bookingService.GetBookingConfirmationAsync(id, userId);

            if (!response.Succeeded)
                return NotFound(response.Message);

            return View(response.Data);
        }

        [HttpGet]
        public async Task<IActionResult> MyTrips()
        {
            int userId = GetCurrentUserId();
            var response = await _bookingService.GetMyTripsAsync(userId);
            return View(response.Data);
        }

        [HttpGet]
        public async Task<IActionResult> HostRequests()
        {
            int userId = GetCurrentUserId();
            var response = await _bookingService.GetHostBookingsAsync(userId);
            return View(response.Data);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            int userId = GetCurrentUserId();
            var response = await _bookingService.GetBookingDetailsAsync(id, userId);

            if (!response.Succeeded)
                return NotFound(response.Message);

            return View(response.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Respond(int id, bool accept)
        {
            int hostId = GetCurrentUserId();
            var response = await _bookingService.RespondToBookingRequestAsync(id, hostId, accept);

            if (response.Succeeded)
                TempData["SuccessMessage"] = response.Message;
            else
                TempData["ErrorMessage"] = response.Message;

            return RedirectToAction(nameof(HostRequests));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string returnUrl)
        {
            int userId = GetCurrentUserId();
            var response = await _bookingService.CancelBookingAsync(id, userId);

            if (response.Succeeded)
                TempData["SuccessMessage"] = response.Message;
            else
                TempData["ErrorMessage"] = response.Message;

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(MyTrips));
        }
    }
}