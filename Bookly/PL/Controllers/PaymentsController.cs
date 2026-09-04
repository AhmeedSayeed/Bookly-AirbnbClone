using BLL.Services.Interfaces;
using DAL.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace PL.Controllers
{
    public class PaymentsController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public PaymentsController(
            IPaymentService paymentService,
            IStringLocalizer<SharedResource> localizer)
        {
            _paymentService = paymentService;
            _localizer = localizer;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(userIdClaim, out int userId);
            return userId;
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(int bookingId, PaymentMethod method = PaymentMethod.Card)
        {
            int userId = GetCurrentUserId();
            var response = await _paymentService.InitiatePaymentAsync(bookingId, userId, method);

            if (!response.Succeeded)
            {
                TempData["ErrorMessage"] = !string.IsNullOrWhiteSpace(response.MessageKey)
                    ? _localizer[response.MessageKey].Value
                    : response.Message;

                return RedirectToAction("Details", "Bookings", new { id = bookingId });
            }

            return Redirect(response.Data);
        }

        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Webhook()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string requestBody = await reader.ReadToEndAsync();

            var result = await _paymentService.ProcessWebhookAsync(requestBody);

            if (result.Succeeded)
                return Ok();

            return BadRequest(result.Message);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Callback(int id, [FromQuery] string? success)
        {
            bool isSuccess = success?.ToLower() == "true";

            if (isSuccess && id > 0)
            {
                await _paymentService.ConfirmPaymentDirectAsync(id);
            }

            string redirectScript = $@"
                    <!DOCTYPE html>
                    <html>
                    <head><title>{_localizer["ProcessingPayment"]}</title></head>
                    <body style='font-family: sans-serif; text-align: center; padding-top: 50px;'>
                        <p>{_localizer["FinalizingBookingPleaseWait"]}</p>
                        <script>
                            // Break out of iframe and navigate the main browser window
                            window.top.location.href = '/Bookings/MyTrips';
                        </script>
                    </body>
                    </html>";

            return Content(redirectScript, "text/html");
        }
    }
}