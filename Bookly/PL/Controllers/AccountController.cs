using AutoMapper;
using BLL.DTOs.Account;
using BLL.DTOs.Auth;
using BLL.Interfaces;
using BLL.Services;
using BLL.Services.Interfaces;
using BLL.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PL.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly IAuthService _authService;
        private readonly IMapper _mapper;

        public AccountController(IUserService userService, IAuthService authService, IMapper mapper)
        {
            _userService = userService;
            _authService = authService;
            _mapper = mapper;
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
                ModelState.AddModelError(string.Empty, response.Message ?? "An error occurred while updating your profile.");
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
                ModelState.AddModelError(string.Empty, response.Message);
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