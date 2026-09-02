using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PL.Controllers
{
    [Authorize]
    public class CouponsController : Controller
    {
        private readonly ICouponService _couponService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public CouponsController(
            ICouponService couponService,
            IStringLocalizer<SharedResource> localizer)
        {
            _couponService = couponService;
            _localizer = localizer;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(userIdClaim, out int userId);
            return userId;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(int bookingId, string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Json(new
                {
                    success = false,
                    message = _localizer["PleaseEnterCouponCode"].Value
                });

            int userId = GetCurrentUserId();
            var response = await _couponService.ApplyCouponToBookingAsync(
                bookingId,
                code,
                userId);

            if (!response.Succeeded || response.Data == null)
            {
                return Json(new
                {
                    success = false,
                    message = !string.IsNullOrEmpty(response.MessageKey)
                        ? _localizer[response.MessageKey].Value
                        : response.Message
                });
            }

            var message = _localizer[response.Data.MessageKey!, response.Data.MessageArgs ?? []].Value;

            return Json(new
            {
                success = true,
                message = message,
                discountPercent = response.Data.DiscountPercent,
                discountAmount = response.Data.DiscountAmount,
                newTotalPrice = response.Data.NewTotalPrice
            });
        }
    }
}