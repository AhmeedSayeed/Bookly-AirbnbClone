
using BLL.Services.Interfaces;
using BLL.ViewModels.Bookings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PL.Controllers
{
    [Authorize]
    public class BookingsController : Controller
    {
        private readonly IBookingService _bookingService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public BookingsController(
            IBookingService bookingService,
            IStringLocalizer<SharedResource> localizer)
        {
            _bookingService = bookingService;
            _localizer = localizer;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            int.TryParse(userIdClaim, out int userId);

            return userId;
        }

        private string GetLocalizedMessage<T>(BLL.DTOs.Response<T> response)
        {
            if (!string.IsNullOrEmpty(response.MessageKey))
            {
                return _localizer[
                    response.MessageKey,
                    response.MessageArguments
                ].Value;
            }

            return response.Message ?? string.Empty;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestBooking(
            BookingRequestViewModel request)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] =
                    _localizer["PleaseFillBookingDetailsCorrectly"].Value;

                return RedirectToAction(
                    "Details",
                    "Listings",
                    new { id = request.ListingId });
            }

            int userId = GetCurrentUserId();

            var response =
                await _bookingService.RequestBookingAsync(userId, request);

            if (!response.Succeeded)
            {
                TempData["BookingError"] =
                    GetLocalizedMessage(response);

                return RedirectToAction(
                    "Details",
                    "Listings",
                    new { id = request.ListingId });
            }

            TempData["SuccessMessage"] =
                GetLocalizedMessage(response);

            return RedirectToAction(
                nameof(Confirmation),
                new { id = response.Data });
        }

        [HttpGet]
        public async Task<IActionResult> Confirmation(int id)
        {
            int userId = GetCurrentUserId();

            var response =
                await _bookingService.GetBookingConfirmationAsync(
                    id,
                    userId);

            if (!response.Succeeded)
                return NotFound(GetLocalizedMessage(response));

            return View(response.Data);
        }

        [HttpGet]
        public async Task<IActionResult> MyTrips()
        {
            int userId = GetCurrentUserId();

            var response =
                await _bookingService.GetMyTripsAsync(userId);

            return View(response.Data);
        }

        [HttpGet]
        public async Task<IActionResult> HostRequests()
        {
            int userId = GetCurrentUserId();

            var response =
                await _bookingService.GetHostBookingsAsync(userId);

            return View(response.Data);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            int userId = GetCurrentUserId();

            var response =
                await _bookingService.GetBookingDetailsAsync(id, userId);

            if (!response.Succeeded)
                return NotFound(GetLocalizedMessage(response));

            return View(response.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Respond(
            int id,
            bool accept)
        {
            int hostId = GetCurrentUserId();

            var response =
                await _bookingService.RespondToBookingRequestAsync(
                    id,
                    hostId,
                    accept);

            if (response.Succeeded)
            {
                TempData["SuccessMessage"] =
                    GetLocalizedMessage(response);
            }
            else
            {
                TempData["ErrorMessage"] =
                    GetLocalizedMessage(response);
            }

            return RedirectToAction(nameof(HostRequests));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(
            int id,
            string returnUrl)
        {
            int userId = GetCurrentUserId();

            var response =
                await _bookingService.CancelBookingAsync(
                    id,
                    userId);

            if (response.Succeeded)
            {
                TempData["SuccessMessage"] =
                    GetLocalizedMessage(response);
            }
            else
            {
                TempData["ErrorMessage"] =
                    GetLocalizedMessage(response);
            }

            if (!string.IsNullOrEmpty(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(MyTrips));
        }
    }
}
