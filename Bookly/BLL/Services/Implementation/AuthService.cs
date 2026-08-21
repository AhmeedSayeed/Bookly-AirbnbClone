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
                return Response<RegisterResultDto>.Fail(
                    ResponseStatus.Conflict,
                    "Registration failed",
                    new List<string> { "Email is already registered." });
            }

            var user = _mapper.Map<ApplicationUser>(model);

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();

                return Response<RegisterResultDto>.Fail(
                    ResponseStatus.ValidationError,
                    "User creation failed",
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

            return Response<RegisterResultDto>.Success(
                registerResult,
                "User registered successfully");
        }

        public async Task<Response<AuthResultDto>> LoginAsync(LoginDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
            {
                return Response<AuthResultDto>.Fail(
                    ResponseStatus.Unauthorized,
                    "Invalid email or password");
            }
            if (!user.EmailConfirmed)
            {
                return Response<AuthResultDto>.Fail(
                    ResponseStatus.Unauthorized,
                    "EmailNotConfirmed");
            }
            // Check if the account is suspended
            if (user.LockoutEnd.HasValue &&
                user.LockoutEnd.Value > DateTimeOffset.UtcNow)
            {
                return Response<AuthResultDto>.Fail(
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

            return Response<AuthResultDto>.Success(
                authResult,
                "Login successful");
        }

        public async Task<Response<AuthResultDto>> RefreshTokenAsync(string currentRefreshToken)
        {
            var storedToken = await _context.Set<RefreshToken>()
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == currentRefreshToken);

            if (storedToken == null)
            {
                return Response<AuthResultDto>.Fail(
                    ResponseStatus.Unauthorized,
                    "Invalid refresh token");
            }

            if (storedToken.ExpiresAt < DateTime.UtcNow ||
                storedToken.RevokedAt != null)
            {
                return Response<AuthResultDto>.Fail(
                    ResponseStatus.Unauthorized,
                    "Token expired or revoked. Please log in again.");
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

            return Response<AuthResultDto>.Success(
                authResult,
                "Token refreshed successfully");
        }

        public async Task<Response<bool>> RevokeTokenAsync(string currentRefreshToken)
        {
            var storedToken = await _context.Set<RefreshToken>()
                .FirstOrDefaultAsync(rt => rt.Token == currentRefreshToken);

            if (storedToken == null || storedToken.RevokedAt != null)
            {
                return Response<bool>.Fail(
                    ResponseStatus.NotFound,
                    "Token not found or already revoked");
            }

            storedToken.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Response<bool>.Success(
                true,
                "Token revoked successfully");
        }
        public async Task<Response<ForgotPasswordResultDto>> ForgotPasswordAsync(
    ForgotPasswordDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null || !user.EmailConfirmed)
            {
                return Response<ForgotPasswordResultDto>.Success(
                    null!,
                    "If an account with that email exists, a reset link has been sent.");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var result = new ForgotPasswordResultDto
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                PasswordResetToken = token
            };

            return Response<ForgotPasswordResultDto>.Success(
                result,
                "Password reset token generated successfully");
        }
        public async Task<Response<bool>> ResetPasswordAsync(
      ResetPasswordDto model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);

            if (user == null)
            {
                return Response<bool>.Fail(
                    ResponseStatus.NotFound,
                    "User not found");
            }

            var result = await _userManager.ResetPasswordAsync(
                user,
                model.Token,
                model.NewPassword);

            if (!result.Succeeded)
            {
                var errors = result.Errors
                    .Select(e => e.Description)
                    .ToList();

                return Response<bool>.Fail(
                    ResponseStatus.ValidationError,
                    "Password reset failed",
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

            return Response<bool>.Success(
                true,
                "Password reset successfully");
        }
    }
}