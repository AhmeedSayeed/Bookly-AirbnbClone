using BLL.DTOs;
using BLL.ViewModels.Admin;
using DAL.Models.Identity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BLL.Services.Interfaces
{
    public interface IAdminService
    {
        // Dashboard uses AdminDashboardViewModel
        Task<Response<AdminDashboardViewModel>> GetDashboardStatsAsync();

        // Verifications
        Task<Response<IEnumerable<HostVerificationRequestViewModel>>> GetPendingVerificationsAsync();
        Task<Response<bool>> ApproveVerificationAsync(int verificationId);
        Task<Response<bool>> RejectVerificationAsync(int verificationId, string reason = null);

        // Users uses AdminUsersViewModel
        Task<Response<AdminUsersViewModel>> GetAllUsersAsync();
        Task<Response<bool>> LockUserAsync(int userId, DateTimeOffset lockoutEnd);
        Task<Response<bool>> UnlockUserAsync(int userId);

        // Listings uses AdminListingsViewModel
        Task<Response<AdminListingsViewModel>> GetAllListingsForModerationAsync();
        Task<Response<bool>> ModerateListingAsync(int listingId, bool isActive);
    }
}