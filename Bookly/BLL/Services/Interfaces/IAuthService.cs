using BLL.DTOs;
using BLL.DTOs.Auth;
using BLL.Settings;
using BLL.ViewModels.Account;
using DAL.Models.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Services.Interfaces
{
    public interface IAuthService
    {
        Task<Response<AuthResultDto>> RegisterAsync(RegisterDto model);
        Task<Response<AuthResultDto>> LoginAsync(LoginDto model);
        Task<Response<AuthResultDto>> RefreshTokenAsync(string currentRefreshToken);
        Task<Response<bool>> RevokeTokenAsync(string currentRefreshToken);
    }
}
