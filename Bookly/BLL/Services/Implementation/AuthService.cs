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

        public AuthService(
            UserManager<ApplicationUser> userManager,
            ITokenService tokenService,
            AppDbContext context,
            IMapper mapper)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _context = context;
            _mapper = mapper;
        }

        public async Task<Response<AuthResultDto>> RegisterAsync(RegisterDto model)
        {
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                return Response<AuthResultDto>.Fail(
                    ResponseStatus.Conflict,
                    "Registration failed",
                    new List<string> { "Email is already registered." });
            }

            var user = _mapper.Map<ApplicationUser>(model);

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return Response<AuthResultDto>.Fail(ResponseStatus.ValidationError, "User creation failed", errors);
            }

            await _userManager.AddToRoleAsync(user, AppRoles.Guest);
            var roles = new List<string> { AppRoles.Guest };

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
                Email = user.Email,
                Roles = roles,
                AccessToken = accessTokenResult.Token,
                AccessTokenExpiration = accessTokenResult.ExpiresAt,
                RefreshToken = refreshTokenResult.Token,
                RefreshTokenExpiration = refreshTokenResult.ExpiresAt
            };

            return Response<AuthResultDto>.Success(authResult, "User registered successfully");
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

            return Response<AuthResultDto>.Success(authResult, "Login successful");
        }

        public async Task<Response<AuthResultDto>> RefreshTokenAsync(string currentRefreshToken)
        {
            var storedToken = await _context.Set<RefreshToken>()
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == currentRefreshToken);

            if (storedToken == null)
            {
                return Response<AuthResultDto>.Fail(ResponseStatus.Unauthorized, "Invalid refresh token");
            }

            if (storedToken.ExpiresAt < DateTime.UtcNow || storedToken.RevokedAt != null)
            {
                return Response<AuthResultDto>.Fail(ResponseStatus.Unauthorized, "Token expired or revoked. Please log in again.");
            }

            var roles = await _userManager.GetRolesAsync(storedToken.User);

            var accessTokenResult = _tokenService.GenerateAccessToken(storedToken.User, roles);
            var newRefreshTokenResult = _tokenService.GenerateRefreshToken();

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

            return Response<AuthResultDto>.Success(authResult, "Token refreshed successfully");
        }

        public async Task<Response<bool>> RevokeTokenAsync(string currentRefreshToken)
        {
            var storedToken = await _context.Set<RefreshToken>()
                .FirstOrDefaultAsync(rt => rt.Token == currentRefreshToken);

            if (storedToken == null || storedToken.RevokedAt != null)
            {
                return Response<bool>.Fail(ResponseStatus.NotFound, "Token not found or already revoked");
            }

            storedToken.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Response<bool>.Success(true, "Token revoked successfully");
        }
    }
}