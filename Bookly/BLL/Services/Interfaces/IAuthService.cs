using BLL.DTOs;
using BLL.DTOs.Auth;
using BLL.Settings;
using BLL.ViewModels.Account;
using DAL.Models.Identity;
using System;
using System.Collections.Generic;
using System.Text;
using static BLL.DTOs.Auth.RegisterResultDto;

namespace BLL.Services.Interfaces
{
    public interface IAuthService
    {
        Task<Response<RegisterResultDto>> RegisterAsync(RegisterDto model);

        Task<Response<AuthResultDto>> LoginAsync(LoginDto model);

        Task<Response<AuthResultDto>> RefreshTokenAsync(string currentRefreshToken);

        Task<Response<bool>> RevokeTokenAsync(string currentRefreshToken);
        Task<Response<ForgotPasswordResultDto>> ForgotPasswordAsync(ForgotPasswordDto model);
        Task<Response<bool>> ResetPasswordAsync(ResetPasswordDto model);
        Task<Response<AuthResultDto>> GoogleLoginAsync(
    string email,
    string firstName,
    string lastName);

    }
}
