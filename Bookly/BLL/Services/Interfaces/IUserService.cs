using BLL.DTOs;
using BLL.DTOs.Account;
using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Services.Interfaces
{
    public interface IUserService
    {
        public Task<Response<ProfileDto>> GetUserProfileAsync(string userId);
        public Task<Response<bool>> UpdateUserProfileAsync(string userId, ProfileDto updatedData);
    }
}
