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
using Microsoft.Extensions.Localization;

namespace PL.Controllers
{
    public class ListingsController : Controller
    {
        private readonly IListingService _listingService;
        private readonly IAmenityService _amenityService;
        private readonly IWishlistService _wishlistService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public ListingsController(
            IListingService listingService,
            IAmenityService amenityService,
            IWishlistService wishlistService,
            UserManager<ApplicationUser> userManager,
            IStringLocalizer<SharedResource> localizer)
        {
            _listingService = listingService;
            _amenityService = amenityService;
            _wishlistService = wishlistService;
            _userManager = userManager;
            _localizer = localizer;
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
            ViewBag.Amenities = amenitiesResponse.Succeeded
                ? amenitiesResponse.Data
                : new List<AmenityDto>();
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

            // Mark which of these listings the current user has already wishlisted,
            // so the heart icon renders filled without an extra request per card.
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var currentUserId = GetCurrentUserId();
                var wishlistedIds = await _wishlistService.GetWishlistedListingIdsAsync(currentUserId);

                foreach (var item in response.Data.Items)
                {
                    item.IsWishlisted = wishlistedIds.Contains(item.Id);
                }
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
                TempData["InfoMessage"] = _localizer["MustBeVerifiedHost"].Value;
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
                TempData["InfoMessage"] = _localizer["VerifyIdToCreateListing"].Value;
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
                TempData["InfoMessage"] =
                    _localizer["MustBeVerifiedHostToPublish"].Value;

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
                ModelState.AddModelError(
                    string.Empty,
                    response.Message ?? _localizer["FailedToCreateListing"].Value
                );

                await LoadAmenitiesIntoViewBagAsync();
                return View(model);
            }

            TempData["SuccessMessage"] =
                _localizer["ListingCreatedSuccessfully"].Value;

            return RedirectToAction(nameof(MyListings));
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            if (!await IsCurrentUserHostAsync())
            {
                TempData["InfoMessage"] =
                    _localizer["MustBeVerifiedHostToEdit"].Value;

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
                TempData["InfoMessage"] =
                    _localizer["MustBeVerifiedHostToEdit"].Value;

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
                ModelState.AddModelError(
                    string.Empty,
                    response.Message ?? _localizer["FailedToUpdateListing"].Value
                );

                await LoadAmenitiesIntoViewBagAsync();
                return View(model);
            }

            TempData["SuccessMessage"] =
                _localizer["ListingUpdatedSuccessfully"].Value;

            return RedirectToAction(nameof(MyListings));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await IsCurrentUserHostAsync())
            {
                TempData["InfoMessage"] =
                    _localizer["MustBeVerifiedHostToDelete"].Value;

                return RedirectToAction("BecomeAHost", "Account");
            }

            var currentUserId = GetCurrentUserId();
            var response = await _listingService.DeleteAsync(id, currentUserId);

            if (!response.Succeeded)
            {
                TempData["ErrorMessage"] =
                    response.Message ?? _localizer["FailedToDeleteListing"].Value;
            }
            else
            {
                TempData["SuccessMessage"] =
                    _localizer["ListingDeletedSuccessfully"].Value;
            }

            return RedirectToAction(nameof(MyListings));
        }
    }
}