using BLL.DTOs;
using BLL.DTOs.Amenity;
using BLL.DTOs.Listing;
using BLL.Services.Interfaces;
using BLL.ViewModels.Listings;
using DAL.Models.Common;
using DAL.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PL.Controllers
{
    public class ListingsController : Controller
    {
        private readonly IListingService _listingService;
        private readonly IAmenityService _amenityService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ListingsController(
            IListingService listingService,
            IAmenityService amenityService,
            UserManager<ApplicationUser> userManager)
        {
            _listingService = listingService;
            _amenityService = amenityService;
            _userManager = userManager;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(userIdClaim, out int userId);
            return userId;
        }

        private async Task<bool> IsCurrentUserHostAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return false;

            var user = await _userManager.FindByIdAsync(userId);
            return user?.IsHost ?? false;
        }

        private async Task LoadAmenitiesIntoViewBagAsync()
        {
            var amenitiesResponse = await _amenityService.GetAllAsync();
            ViewBag.Amenities = amenitiesResponse.Succeeded ? amenitiesResponse.Data : new List<AmenityDto>();
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] ListingSearchRequestDto searchRequest)
        {
            await LoadAmenitiesIntoViewBagAsync();

            var response = await _listingService.SearchListingsAsync(searchRequest);

            ViewBag.CurrentSearch = searchRequest;

            if (!response.Succeeded || response.Data == null)
            {
                return View(new PagedResult<ListingCardDto>());
            }

            return View(response.Data);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var response = await _listingService.GetDetailsAsync(id);

            if (!response.Succeeded)
                return NotFound(response.Message);

            return View(response.Data);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> MyListings()
        {
            if (!await IsCurrentUserHostAsync())
            {
                TempData["InfoMessage"] = "You must be a verified host to manage listings.";
                return RedirectToAction("BecomeAHost", "Account");
            }

            var currentUserId = GetCurrentUserId();
            var response = await _listingService.GetListingsByHostIdAsync(currentUserId);

            return View(response.Data);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Create()
        {
            if (!await IsCurrentUserHostAsync())
            {
                TempData["InfoMessage"] = "Ready to start earning? Verify your ID to create a listing.";
                return RedirectToAction("BecomeAHost", "Account");
            }

            await LoadAmenitiesIntoViewBagAsync();
            return View(new ListingFormViewModel());
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ListingFormViewModel model)
        {
            if (!await IsCurrentUserHostAsync())
            {
                TempData["InfoMessage"] = "You must be a verified host to publish a listing.";
                return RedirectToAction("BecomeAHost", "Account");
            }

            if (!ModelState.IsValid)
            {
                await LoadAmenitiesIntoViewBagAsync();
                return View(model);
            }

            var currentUserId = GetCurrentUserId();
            var response = await _listingService.CreateAsync(model, currentUserId);

            if (!response.Succeeded)
            {
                ModelState.AddModelError(string.Empty, response.Message ?? "Failed to create listing.");
                await LoadAmenitiesIntoViewBagAsync();
                return View(model);
            }

            TempData["SuccessMessage"] = "Listing created successfully.";
            return RedirectToAction(nameof(MyListings));
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            if (!await IsCurrentUserHostAsync())
            {
                TempData["InfoMessage"] = "You must be a verified host to edit listings.";
                return RedirectToAction("BecomeAHost", "Account");
            }

            var response = await _listingService.GetListingForEditAsync(id);

            if (!response.Succeeded)
                return NotFound(response.Message);

            await LoadAmenitiesIntoViewBagAsync();
            return View(response.Data);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ListingFormViewModel model)
        {
            if (!await IsCurrentUserHostAsync())
            {
                TempData["InfoMessage"] = "You must be a verified host to edit listings.";
                return RedirectToAction("BecomeAHost", "Account");
            }

            if (!ModelState.IsValid)
            {
                await LoadAmenitiesIntoViewBagAsync();
                return View(model);
            }

            var currentUserId = GetCurrentUserId();
            var response = await _listingService.UpdateAsync(model, currentUserId);

            if (!response.Succeeded)
            {
                ModelState.AddModelError(string.Empty, response.Message ?? "Failed to update listing.");
                await LoadAmenitiesIntoViewBagAsync();
                return View(model);
            }

            TempData["SuccessMessage"] = "Listing updated successfully.";
            return RedirectToAction(nameof(MyListings));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await IsCurrentUserHostAsync())
            {
                TempData["InfoMessage"] = "You must be a verified host to delete listings.";
                return RedirectToAction("BecomeAHost", "Account");
            }

            var currentUserId = GetCurrentUserId();
            var response = await _listingService.DeleteAsync(id, currentUserId);

            if (!response.Succeeded)
            {
                TempData["ErrorMessage"] = response.Message ?? "Failed to delete listing.";
            }
            else
            {
                TempData["SuccessMessage"] = "Listing deleted successfully.";
            }

            return RedirectToAction(nameof(MyListings));
        }
    }
}