using AutoMapper;
using BLL.DTOs.Account;
using BLL.DTOs.Auth;
using BLL.Interfaces;
using BLL.Services;
using BLL.Services.Implementation;
using BLL.Services.Interfaces;
using BLL.ViewModels.Account;
using DAL.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;

namespace PL.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly IAuthService _authService;
        private readonly IMapper _mapper;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IVerificationService _verificationService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public AccountController(UserManager<ApplicationUser> userManager,
            IUserService userService, IAuthService authService, IVerificationService verificationService, IMapper mapper, IStringLocalizer<SharedResource> localizer)
        {
            _userManager = userManager;
            _userService = userService;
            _authService = authService;
            _verificationService = verificationService;
            _mapper = mapper;
            _localizer = localizer;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            var response = await _userService.GetUserProfileAsync(userId);

            if (!response.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }

            var viewModel = _mapper.Map<ProfileViewModel>(response.Data);
            return View(viewModel);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (currentUserId != model.Id.ToString())
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var dto = _mapper.Map<ProfileDto>(model);

            var response = await _userService.UpdateUserProfileAsync(currentUserId, dto);

            if (!response.Succeeded)
            {
                ModelState.AddModelError(
                    string.Empty,
                    _localizer["ProfileUpdateError"].Value
                );

                return View(model);
            }

            return RedirectToAction(nameof(Profile));
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var dto = _mapper.Map<RegisterDto>(model);
            var response = await _authService.RegisterAsync(dto);

            if (!response.Succeeded)
            {
                foreach (var error in response.Errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }

                return View(model);
            }

            SetTokenCookies(response.Data, false);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var dto = new LoginDto
            {
                Email = model.Email,
                Password = model.Password,
                RememberMe = model.RememberMe
            };

            var response = await _authService.LoginAsync(dto);

            if (!response.Succeeded)
            {
                var message = response.Message == "AccountSuspended"
                    ? _localizer["AccountSuspended"].Value
                    : _localizer["InvalidEmailOrPassword"].Value;

                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            SetTokenCookies(response.Data, model.RememberMe);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refresh_token"];

            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _authService.RevokeTokenAsync(refreshToken);
            }

            Response.Cookies.Delete("access_token");
            Response.Cookies.Delete("refresh_token");

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> BecomeAHost()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out int userId)) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user != null && user.IsHost)
            {
                return RedirectToAction("MyListings", "Listings");
            }

            var verificationResponse = await _verificationService.GetVerificationByUserIdAsync(userId);
            var verification = verificationResponse.Data;

            if (verification != null)
            {
                ViewBag.VerificationStatus = verification.Status.ToString();
            }

            return View(new BecomeAHostViewModel());
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BecomeAHost(BecomeAHostViewModel model)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out int userId)) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user != null && user.IsHost)
                return RedirectToAction("MyListings", "Listings");

            if (!ModelState.IsValid)
            {
                var verificationResponse =
                    await _verificationService.GetVerificationByUserIdAsync(userId);

                if (verificationResponse.Data != null)
                    ViewBag.VerificationStatus =
                        verificationResponse.Data.Status.ToString();

                return View(model);
            }

            var response =
                await _verificationService.SubmitVerificationAsync(userId, model);

            if (!response.Succeeded)
            {
                ModelState.AddModelError(
                    string.Empty,
                    _localizer["ProfileUpdateError"].Value
                );

                return View(model);
            }

            TempData["SuccessMessage"] =
                _localizer["IdSubmittedForReview"].Value;

            return RedirectToAction(nameof(BecomeAHost));
        }

        private void SetTokenCookies(AuthResultDto data, bool rememberMe)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                IsEssential = true
            };

            if (rememberMe)
            {
                var accessOptions = cookieOptions;
                accessOptions.Expires = data.AccessTokenExpiration;
                Response.Cookies.Append("access_token", data.AccessToken, accessOptions);

                var refreshOptions = cookieOptions;
                refreshOptions.Expires = data.RefreshTokenExpiration;
                Response.Cookies.Append("refresh_token", data.RefreshToken, refreshOptions);
            }
            else
            {
                Response.Cookies.Append("access_token", data.AccessToken, cookieOptions);
                Response.Cookies.Append("refresh_token", data.RefreshToken, cookieOptions);
            }
        }
    }
}