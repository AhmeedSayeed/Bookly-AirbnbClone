using AutoMapper;
using BLL.DTOs;
using BLL.DTOs.Auth;
using BLL.Interfaces;
using BLL.Services.Interfaces;
using DAL;
using DAL.Constants;
using DAL.Models;
using DAL.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            ITokenService tokenService,
            AppDbContext context,
            IMapper mapper,
            IEmailService emailService)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _context = context;
            _mapper = mapper;
            _emailService = emailService;
        }

        public async Task<Response<RegisterResultDto>> RegisterAsync(RegisterDto model)
        {
            var existingUser = await _userManager.FindByEmailAsync(model.Email);

            if (existingUser != null)
            {
                return Response<RegisterResultDto>.FailWithKey(
                    ResponseStatus.Conflict,
                    "RegistrationFailed",
                    new List<string> { "EmailAlreadyRegistered" });
            }

            var user = _mapper.Map<ApplicationUser>(model);

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                var errors = result.Errors
                    .Select(e => e.Code)
                    .ToList();

                return Response<RegisterResultDto>.FailWithKey(
                    ResponseStatus.ValidationError,
                    "UserCreationFailed",
                    errors);
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            var registerResult = new RegisterResultDto
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                EmailConfirmationToken = token
            };

            await _userManager.AddToRoleAsync(user, AppRoles.Guest);

            return Response<RegisterResultDto>.SuccessWithKey(
                registerResult,
                "UserRegisteredSuccessfully");
        }

        public async Task<Response<AuthResultDto>> LoginAsync(LoginDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
            {
                return Response<AuthResultDto>.FailWithKey(
                    ResponseStatus.Unauthorized,
                    "InvalidEmailOrPassword");
            }

            if (!user.EmailConfirmed)
            {
                return Response<AuthResultDto>.FailWithKey(
                    ResponseStatus.Unauthorized,
                    "EmailNotConfirmed");
            }

            // Check if the account is suspended
            if (user.LockoutEnd.HasValue &&
                user.LockoutEnd.Value > DateTimeOffset.UtcNow)
            {
                return Response<AuthResultDto>.FailWithKey(
                    ResponseStatus.Unauthorized,
                    "AccountSuspended");
            }

            var roles = await _userManager.GetRolesAsync(user);

            var accessTokenResult = _tokenService.GenerateAccessToken(user, roles);
            var refreshTokenResult = _tokenService.GenerateRefreshToken();

            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshTokenResult.Token,
                ExpiresAt = refreshTokenResult.ExpiresAt,
                CreatedAt = DateTime.UtcNow
            };

            _context.Set<RefreshToken>().Add(refreshToken);
            await _context.SaveChangesAsync();

            var authResult = new AuthResultDto
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                Roles = roles,
                AccessToken = accessTokenResult.Token,
                AccessTokenExpiration = accessTokenResult.ExpiresAt,
                RefreshToken = refreshTokenResult.Token,
                RefreshTokenExpiration = refreshTokenResult.ExpiresAt
            };

            return Response<AuthResultDto>.SuccessWithKey(
                authResult,
                "LoginSuccessful");
        }

        public async Task<Response<AuthResultDto>> GoogleLoginAsync(
            string email,
            string firstName,
            string lastName)
        {
            // 1. Find user by email
            var user = await _userManager.FindByEmailAsync(email);

            // 2. If user doesn't exist, create a new account
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow,
                    IsHost = false,
                    IsDeleted = false
                };

                var createResult = await _userManager.CreateAsync(user);

                if (!createResult.Succeeded)
                {
                    var errors = createResult.Errors
                        .Select(e => e.Code)
                        .ToList();

                    return Response<AuthResultDto>.FailWithKey(
                        ResponseStatus.ValidationError,
                        "GoogleAccountCreationFailed",
                        errors);
                }

                // New Google users are Guests by default
                await _userManager.AddToRoleAsync(user, AppRoles.Guest);
            }

            // 3. Check if account is suspended
            if (user.LockoutEnd.HasValue &&
                user.LockoutEnd.Value > DateTimeOffset.UtcNow)
            {
                return Response<AuthResultDto>.FailWithKey(
                    ResponseStatus.Unauthorized,
                    "AccountSuspended");
            }

            // 4. Get user's roles
            var roles = await _userManager.GetRolesAsync(user);

            // 5. Generate JWT
            var accessTokenResult =
                _tokenService.GenerateAccessToken(user, roles);

            // 6. Generate Refresh Token
            var refreshTokenResult =
                _tokenService.GenerateRefreshToken();

            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshTokenResult.Token,
                ExpiresAt = refreshTokenResult.ExpiresAt,
                CreatedAt = DateTime.UtcNow
            };

            _context.Set<RefreshToken>().Add(refreshToken);
            await _context.SaveChangesAsync();

            // 7. Build authentication result
            var authResult = new AuthResultDto
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                Roles = roles,
                AccessToken = accessTokenResult.Token,
                AccessTokenExpiration = accessTokenResult.ExpiresAt,
                RefreshToken = refreshTokenResult.Token,
                RefreshTokenExpiration = refreshTokenResult.ExpiresAt
            };

            return Response<AuthResultDto>.SuccessWithKey(
                authResult,
                "GoogleLoginSuccessful");
        }

        public async Task<Response<AuthResultDto>> RefreshTokenAsync(string currentRefreshToken)
        {
            var storedToken = await _context.Set<RefreshToken>()
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == currentRefreshToken);

            if (storedToken == null)
            {
                return Response<AuthResultDto>.FailWithKey(
                    ResponseStatus.Unauthorized,
                    "InvalidRefreshToken");
            }

            if (storedToken.ExpiresAt < DateTime.UtcNow ||
                storedToken.RevokedAt != null)
            {
                return Response<AuthResultDto>.FailWithKey(
                    ResponseStatus.Unauthorized,
                    "TokenExpiredOrRevoked");
            }

            var roles = await _userManager.GetRolesAsync(storedToken.User);

            var accessTokenResult =
                _tokenService.GenerateAccessToken(storedToken.User, roles);

            var newRefreshTokenResult =
                _tokenService.GenerateRefreshToken();

            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.ReplacedByToken = newRefreshTokenResult.Token;

            var newRefreshToken = new RefreshToken
            {
                UserId = storedToken.UserId,
                Token = newRefreshTokenResult.Token,
                ExpiresAt = newRefreshTokenResult.ExpiresAt,
                CreatedAt = DateTime.UtcNow
            };

            _context.Set<RefreshToken>().Add(newRefreshToken);
            await _context.SaveChangesAsync();

            var authResult = new AuthResultDto
            {
                UserId = storedToken.User.Id,
                FirstName = storedToken.User.FirstName,
                LastName = storedToken.User.LastName,
                Email = storedToken.User.Email ?? string.Empty,
                Roles = roles,
                AccessToken = accessTokenResult.Token,
                AccessTokenExpiration = accessTokenResult.ExpiresAt,
                RefreshToken = newRefreshTokenResult.Token,
                RefreshTokenExpiration = newRefreshTokenResult.ExpiresAt
            };

            return Response<AuthResultDto>.SuccessWithKey(
                authResult,
                "TokenRefreshedSuccessfully");
        }

        public async Task<Response<bool>> RevokeTokenAsync(string currentRefreshToken)
        {
            var storedToken = await _context.Set<RefreshToken>()
                .FirstOrDefaultAsync(rt => rt.Token == currentRefreshToken);

            if (storedToken == null || storedToken.RevokedAt != null)
            {
                return Response<bool>.FailWithKey(
                    ResponseStatus.NotFound,
                    "TokenNotFoundOrAlreadyRevoked");
            }

            storedToken.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Response<bool>.SuccessWithKey(
                true,
                "TokenRevokedSuccessfully");
        }

        public async Task<Response<ForgotPasswordResultDto>> ForgotPasswordAsync(
            ForgotPasswordDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null || !user.EmailConfirmed)
            {
                return Response<ForgotPasswordResultDto>.SuccessWithKey(
                    null!,
                    "PasswordResetLinkSentIfAccountExists");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var result = new ForgotPasswordResultDto
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                PasswordResetToken = token
            };

            return Response<ForgotPasswordResultDto>.SuccessWithKey(
                result,
                "PasswordResetTokenGeneratedSuccessfully");
        }

        public async Task<Response<bool>> ResetPasswordAsync(
            ResetPasswordDto model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);

            if (user == null)
            {
                return Response<bool>.FailWithKey(
                    ResponseStatus.NotFound,
                    "UserNotFound");
            }

            var result = await _userManager.ResetPasswordAsync(
                user,
                model.Token,
                model.NewPassword);

            if (!result.Succeeded)
            {
                var errors = result.Errors
                    .Select(e => e.Code)
                    .ToList();

                return Response<bool>.FailWithKey(
                    ResponseStatus.ValidationError,
                    "PasswordResetFailed",
                    errors);
            }

            var activeTokens = await _context.Set<RefreshToken>()
                .Where(rt => rt.UserId == user.Id && rt.RevokedAt == null)
                .ToListAsync();

            foreach (var rt in activeTokens)
            {
                rt.RevokedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Response<bool>.SuccessWithKey(
                true,
                "PasswordResetSuccessfully");
        }
    }
}