using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;

namespace PL.Middlewares
{
    public class RefreshTokenMiddleware
    {
        private readonly RequestDelegate _next;

        public RefreshTokenMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IAuthService authService)
        {
            var accessToken = context.Request.Cookies["access_token"];
            var refreshToken = context.Request.Cookies["refresh_token"];

            if (!string.IsNullOrEmpty(refreshToken))
            {
                bool needsRefresh = false;

                if (string.IsNullOrEmpty(accessToken))
                {
                    needsRefresh = true;
                }
                else
                {
                    var handler = new JwtSecurityTokenHandler();
                    if (handler.CanReadToken(accessToken))
                    {
                        var jwtToken = handler.ReadJwtToken(accessToken);

                        if (jwtToken.ValidTo < DateTime.UtcNow.AddMinutes(1))
                        {
                            needsRefresh = true;
                        }
                    }
                }

                if (needsRefresh)
                {
                    var response = await authService.RefreshTokenAsync(refreshToken);

                    if (response.Succeeded && response.Data != null)
                    {
                        var cookieOptions = new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true,
                            SameSite = SameSiteMode.Lax,
                            IsEssential = true
                        };

                        var accessOptions = new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true,
                            SameSite = SameSiteMode.Lax,
                            IsEssential = true,
                            Expires = response.Data.AccessTokenExpiration
                        };

                        var refreshOptions = new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true,
                            SameSite = SameSiteMode.Lax,
                            IsEssential = true,
                            Expires = response.Data.RefreshTokenExpiration
                        };

                        context.Response.Cookies.Append("access_token", response.Data.AccessToken, accessOptions);
                        context.Response.Cookies.Append("refresh_token", response.Data.RefreshToken, refreshOptions);

                        context.Request.Headers["Authorization"] = "Bearer " + response.Data.AccessToken;
                    }
                }
            }

            await _next(context);
        }
    }
}