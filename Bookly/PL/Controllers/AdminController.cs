using BLL.Services.Interfaces;
using BLL.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;

namespace PL.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly ICouponService _couponService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public AdminController(
            IAdminService adminService,
            ICouponService couponService,
            IStringLocalizer<SharedResource> localizer)
        {
            _adminService = adminService;
            _couponService = couponService;
            _localizer = localizer;
        }


        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var response = await _adminService.GetDashboardStatsAsync();

            if (!response.Succeeded)
            {
                TempData["ErrorMessage"] =
                    _localizer["FailedToLoadDashboardStatistics"].Value;

                return View(new AdminDashboardViewModel());
            }

            return View(response.Data);
        }


        [HttpGet]
        public async Task<IActionResult> Verifications()
        {
            var response = await _adminService.GetPendingVerificationsAsync();

            return View(response.Data);
        }


        [HttpGet]
        public IActionResult ViewDocument(
            string url,
            [FromServices] IWebHostEnvironment env)
        {
            if (string.IsNullOrWhiteSpace(url))
                return View("DocumentNotFound");

            var relativePath = url.TrimStart('/', '\\');
            var physicalPath = Path.Combine(
                env.WebRootPath,
                relativePath
            );

            if (!System.IO.File.Exists(physicalPath))
                return View("DocumentNotFound");

            var provider = new FileExtensionContentTypeProvider();

            if (!provider.TryGetContentType(
                    physicalPath,
                    out var contentType))
            {
                contentType = "application/octet-stream";
            }

            return PhysicalFile(
                physicalPath,
                contentType
            );
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveVerification(int id)
        {
            var response =
                await _adminService.ApproveVerificationAsync(id);

            TempData[
                response.Succeeded
                    ? "SuccessMessage"
                    : "ErrorMessage"
            ] = response.Message == null
                ? null
                : _localizer[response.Message].Value;

            return RedirectToAction(nameof(Verifications));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectVerification(
            int id,
            string reason)
        {
            var response =
                await _adminService.RejectVerificationAsync(
                    id,
                    reason
                );

            TempData[
                response.Succeeded
                    ? "SuccessMessage"
                    : "ErrorMessage"
            ] = response.Message == null
                ? null
                : _localizer[response.Message].Value;

            return RedirectToAction(nameof(Verifications));
        }


        [HttpGet]
        public async Task<IActionResult> Users()
        {
            var response =
                await _adminService.GetAllUsersAsync();

            return View(response.Data);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockUser(int id)
        {
            var response =
                await _adminService.LockUserAsync(
                    id,
                    DateTimeOffset.UtcNow.AddYears(100)
                );

            TempData[
                response.Succeeded
                    ? "SuccessMessage"
                    : "ErrorMessage"
            ] = response.Message == null
                ? null
                : _localizer[response.Message].Value;

            return RedirectToAction(nameof(Users));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockUser(int id)
        {
            var response =
                await _adminService.UnlockUserAsync(id);

            TempData[
                response.Succeeded
                    ? "SuccessMessage"
                    : "ErrorMessage"
            ] = response.Message == null
                ? null
                : _localizer[response.Message].Value;

            return RedirectToAction(nameof(Users));
        }


        [HttpGet]
        public async Task<IActionResult> Listings()
        {
            var response =
                await _adminService.GetAllListingsForModerationAsync();

            return View(response.Data);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ModerateListing(
            int id,
            bool isActive)
        {
            var response =
                await _adminService.ModerateListingAsync(
                    id,
                    isActive
                );

            TempData[
                response.Succeeded
                    ? "SuccessMessage"
                    : "ErrorMessage"
            ] = response.Message == null
                ? null
                : _localizer[response.Message].Value;

            return RedirectToAction(nameof(Listings));
        }

        [HttpGet]
        public async Task<IActionResult> Coupons()
        {
            var response = await _couponService.GetAllCouponsAsync();
            return View(response.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCoupon(BLL.DTOs.CreateCouponDto model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fill in all coupon fields correctly.";
                return RedirectToAction(nameof(Coupons));
            }

            var response = await _couponService.CreateCouponAsync(model);
            TempData[response.Succeeded ? "SuccessMessage" : "ErrorMessage"] = response.Message;
            return RedirectToAction(nameof(Coupons));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCoupon(int id)
        {
            var response = await _couponService.DeleteCouponAsync(id);
            TempData[response.Succeeded ? "SuccessMessage" : "ErrorMessage"] = response.Message;
            return RedirectToAction(nameof(Coupons));
        }
    }
}