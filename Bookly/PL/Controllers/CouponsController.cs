using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PL.Controllers
{
    [Authorize]
    public class CouponsController : Controller
    {
        private readonly ICouponService _couponService;

        public CouponsController(ICouponService couponService)
        {
            _couponService = couponService;
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
                return Json(new { success = false, message = "Please enter a valid coupon code." });

            int userId = GetCurrentUserId();
            var response = await _couponService.ApplyCouponToBookingAsync(bookingId, code, userId);

            if (!response.Succeeded || response.Data == null)
            {
                return Json(new { success = false, message = response.Message });
            }

            return Json(new
            {
                success = true,
                message = response.Data.Message,
                discountPercent = response.Data.DiscountPercent,
                discountAmount = response.Data.DiscountAmount,
                newTotalPrice = response.Data.NewTotalPrice
            });
        }
    }
}
