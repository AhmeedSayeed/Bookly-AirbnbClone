using AutoMapper;
using BLL.DTOs;
using BLL.DTOs.Account;
using BLL.Services.Interfaces;
using DAL.Models.Identity;
using Microsoft.AspNetCore.Identity;

namespace BLL.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly IFileUploader _fileUploader;

        public UserService(UserManager<ApplicationUser> userManager, IMapper mapper, IFileUploader fileUploader)
        {
            _userManager = userManager;
            _mapper = mapper;
            _fileUploader = fileUploader;
        }

        public async Task<Response<ProfileDto>> GetUserProfileAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return Response<ProfileDto>.Fail(ResponseStatus.NotFound, "User not found.");
            }

            var profileDto = _mapper.Map<ProfileDto>(user);
            return Response<ProfileDto>.Success(profileDto);
        }

        public async Task<Response<bool>> UpdateUserProfileAsync(string userId, ProfileDto updatedData)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return Response<bool>.Fail(ResponseStatus.NotFound, "User not found.");
            }

            user.FirstName = updatedData.FirstName;
            user.LastName = updatedData.LastName;
            user.Bio = updatedData.Bio;

            if (updatedData.ProfilePhoto != null)
            {
                try
                {
                    if (!string.IsNullOrEmpty(user.ProfilePhotoUrl))
                    {
                        _fileUploader.DeleteFile(user.ProfilePhotoUrl);
                    }

                    var newPhotoUrl = await _fileUploader.SaveFileAsync(updatedData.ProfilePhoto, "profile-photos");
                    user.ProfilePhotoUrl = newPhotoUrl;
                }
                catch (Exception ex)
                {
                    return Response<bool>.Fail(ResponseStatus.Error, $"File upload failed: {ex.Message}");
                }
            }

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var error = result.Errors.FirstOrDefault()?.Description ?? "Failed to update profile.";
                return Response<bool>.Fail(ResponseStatus.Error, error);
            }

            return Response<bool>.Success(true);
        }
    }
}