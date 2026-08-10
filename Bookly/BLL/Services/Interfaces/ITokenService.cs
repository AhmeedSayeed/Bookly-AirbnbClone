using BLL.DTOs.Auth;
using DAL.Models;
using DAL.Models.Identity;
using System.Collections.Generic;

namespace BLL.Interfaces
{
    public interface ITokenService
    {
        AccessTokenResult GenerateAccessToken(ApplicationUser user, IList<string> roles);
        RefreshTokenResult GenerateRefreshToken();
    }
}