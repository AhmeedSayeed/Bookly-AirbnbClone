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
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;

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
        private readonly IEmailService _emailService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            IUserService userService,
            IAuthService authService,
            IVerificationService verificationService,
            IMapper mapper,
            IStringLocalizer<SharedResource> localizer,
            IEmailService emailService)
        {
            _userManager = userManager;
            _userService = userService;
            _authService = authService;
            _verificationService = verificationService;
            _mapper = mapper;
            _localizer = localizer;
            _emailService = emailService;
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
                    ModelState.AddModelError(
                        string.Empty,
                        _localizer[error].Value
                    );
                }

                if (!string.IsNullOrEmpty(response.MessageKey))
                {
                    ModelState.AddModelError(
                        string.Empty,
                        _localizer[response.MessageKey].Value
                    );
                }

                return View(model);
            }

            var confirmationLink = Url.Action(
                nameof(ConfirmEmail),
                "Account",
                new
                {
                    userId = response.Data.UserId,
                    token = response.Data.EmailConfirmationToken
                },
                Request.Scheme);

            try
            {
                await _emailService.SendEmailAsync(
                    response.Data.Email,
                    _localizer["ConfirmBooklyAccountSubject"].Value,
                    _localizer["ConfirmBooklyAccountBody", confirmationLink].Value
                );
            }
            catch (Exception)
            {
                // Error in sending email
            }

            return RedirectToAction(nameof(CheckEmail));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult CheckEmail()
        {
            return View();
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
                var message = !string.IsNullOrEmpty(response.MessageKey)
                    ? _localizer[response.MessageKey].Value
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
        [AllowAnonymous]
        public IActionResult GoogleLogin()
        {
            var redirectUrl = Url.Action(
                nameof(GoogleResponse),
                "Account");

            var properties = new AuthenticationProperties
            {
                RedirectUri = redirectUrl
            };

            return Challenge(
                properties,
                GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(
                GoogleDefaults.AuthenticationScheme);

            if (!result.Succeeded)
            {
                return RedirectToAction(nameof(Login));
            }

            var email = result.Principal?.FindFirstValue(ClaimTypes.Email);
            var firstName = result.Principal?.FindFirstValue(ClaimTypes.GivenName);
            var lastName = result.Principal?.FindFirstValue(ClaimTypes.Surname);

            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction(nameof(Login));
            }

            var response = await _authService.GoogleLoginAsync(
                email,
                firstName ?? "",
                lastName ?? "");

            if (!response.Succeeded || response.Data == null)
            {
                TempData["GoogleLoginError"] =
                    !string.IsNullOrEmpty(response.MessageKey)
                        ? _localizer[response.MessageKey].Value
                        : _localizer["GoogleLoginFailed"].Value;

                return RedirectToAction(nameof(Login));
            }

            // Create Bookly JWT cookies
            SetTokenCookies(response.Data, true);

            // Clear the temporary external authentication cookie
            await HttpContext.SignOutAsync(
                IdentityConstants.ExternalScheme);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> BecomeAHost()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user != null && user.IsHost)
            {
                return RedirectToAction("MyListings", "Listings");
            }

            var verificationResponse =
                await _verificationService.GetVerificationByUserIdAsync(userId);

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
        public async Task<IActionResult> BecomeAHost(
            BecomeAHostViewModel model)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user != null && user.IsHost)
                return RedirectToAction("MyListings", "Listings");

            if (!ModelState.IsValid)
            {
                var verificationResponse =
                    await _verificationService.GetVerificationByUserIdAsync(userId);

                if (verificationResponse.Data != null)
                {
                    ViewBag.VerificationStatus =
                        verificationResponse.Data.Status.ToString();
                }

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

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private void SetTokenCookies(
            AuthResultDto data,
            bool rememberMe)
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

                Response.Cookies.Append(
                    "access_token",
                    data.AccessToken,
                    accessOptions);

                var refreshOptions = cookieOptions;
                refreshOptions.Expires = data.RefreshTokenExpiration;

                Response.Cookies.Append(
                    "refresh_token",
                    data.RefreshToken,
                    refreshOptions);
            }
            else
            {
                Response.Cookies.Append(
                    "access_token",
                    data.AccessToken,
                    cookieOptions);

                Response.Cookies.Append(
                    "refresh_token",
                    data.RefreshToken,
                    cookieOptions);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(
            string userId,
            string token)
        {
            if (string.IsNullOrEmpty(userId) ||
                string.IsNullOrEmpty(token))
            {
                return RedirectToAction(nameof(Login));
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            if (user.EmailConfirmed)
            {
                return View(true);
            }

            var result =
                await _userManager.ConfirmEmailAsync(user, token);

            if (!result.Succeeded)
            {
                return View(false);
            }

            return View(true);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(
            ForgotPasswordDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var response =
                await _authService.ForgotPasswordAsync(model);

            if (response.Succeeded && response.Data != null)
            {
                var resetLink = Url.Action(
                    "ResetPassword",
                    "Account",
                    new
                    {
                        userId = response.Data.UserId,
                        token = response.Data.PasswordResetToken
                    },
                    Request.Scheme);

                await _emailService.SendEmailAsync(
                    response.Data.Email,
                    _localizer["ResetBooklyPasswordSubject"].Value,
                    _localizer["ResetBooklyPasswordBody", resetLink].Value
                );
            }

            return RedirectToAction(nameof(CheckResetEmail));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(
            string userId,
            string token)
        {
            var model = new ResetPasswordDto
            {
                UserId = userId,
                Token = token
            };

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            ResetPasswordDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var response =
                await _authService.ResetPasswordAsync(model);

            if (!response.Succeeded)
            {
                foreach (var error in response.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        _localizer[error].Value
                    );
                }

                if (!string.IsNullOrEmpty(response.MessageKey))
                {
                    ModelState.AddModelError(
                        string.Empty,
                        _localizer[response.MessageKey].Value
                    );
                }

                return View(model);
            }

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult CheckResetEmail()
        {
            return View();
        }
    }
}

