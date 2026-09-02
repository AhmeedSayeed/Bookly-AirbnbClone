using BLL.DTOs;
using BLL.Services.Interfaces;
using BLL.ViewModels.Admin;
using DAL.Enums;
using DAL.Models.Identity;
using DAL.Models.Property;
using DAL.Models.Reservations;
using DAL.Repository.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Services.Implementation
{
    public class AdminService : IAdminService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;

        private readonly IRepository<HostVerification> _verificationRepo;
        private readonly IRepository<Listing> _listingRepo;
        private readonly IRepository<Booking> _bookingRepo;
        private readonly IRepository<Payment> _paymentRepo;
        private readonly IRepository<RefreshToken> _tokenRepo;

        public AdminService(
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService,
            IRepository<HostVerification> verificationRepo,
            IRepository<Listing> listingRepo,
            IRepository<Booking> bookingRepo,
            IRepository<Payment> paymentRepo,
            IRepository<RefreshToken> tokenRepo)
        {
            _userManager = userManager;
            _notificationService = notificationService;
            _verificationRepo = verificationRepo;
            _listingRepo = listingRepo;
            _bookingRepo = bookingRepo;
            _paymentRepo = paymentRepo;
            _tokenRepo = tokenRepo;
        }


        // =========================================================
        // HOST VERIFICATIONS
        // =========================================================

        public async Task<Response<IEnumerable<HostVerificationRequestViewModel>>> GetPendingVerificationsAsync()
        {
            var pending = await _verificationRepo.GetAllAsync(
                selector: v => v,
                filter: v => v.Status == HostVerificationStatus.Pending,
                orderBy: q => q.OrderBy(v => v.SubmittedAt),
                Includes: v => v.User
            );

            var viewModels = pending.Select(v => new HostVerificationRequestViewModel
            {
                Id = v.Id,
                UserId = v.UserId,
                HostName = $"{v.User?.FirstName} {v.User?.LastName}".Trim(),
                DocumentUrl = v.DocumentUrl,
                SubmittedAt = v.SubmittedAt
            }).ToList();

            return Response<IEnumerable<HostVerificationRequestViewModel>>.Success(viewModels);
        }


        public async Task<Response<bool>> ApproveVerificationAsync(int verificationId)
        {
            var verification = await _verificationRepo.GetAsync(
                selector: v => v,
                filter: v => v.Id == verificationId,
                Includes: v => v.User
            );

            if (verification == null ||
                verification.Status != HostVerificationStatus.Pending)
            {
                return Response<bool>.Fail(
                    ResponseStatus.NotFound,
                    "VerificationNotFoundOrProcessed"
                );
            }

            verification.Status = HostVerificationStatus.Verified;
            verification.VerifiedAt = DateTime.UtcNow;
            verification.User.IsHost = true;

            var roleResult = await _userManager.AddToRoleAsync(
                verification.User,
                "Host"
            );

            if (!roleResult.Succeeded)
            {
                return Response<bool>.Fail(
                    ResponseStatus.Error,
                    "HostRoleAssignmentFailed",
                    roleResult.Errors
                        .Select(e => e.Description)
                        .ToList()
                );
            }

            _verificationRepo.Update(verification);
            await _verificationRepo.SaveAsync();

            await _notificationService.SendNotificationAsync(
     verification.UserId,
     "HostVerificationApprovedNotification",
     null,
     "/Listings/Create"
 );
            return Response<bool>.Success(
                true,
                "HostApprovedSuccessfully"
            );
        }


        public async Task<Response<bool>> RejectVerificationAsync(
            int verificationId,
            string reason = null)
        {
            var verification = await _verificationRepo.GetAsync(
                selector: v => v,
                filter: v => v.Id == verificationId,
                Includes: v => v.User
            );

            if (verification == null ||
                verification.Status != HostVerificationStatus.Pending)
            {
                return Response<bool>.Fail(
                    ResponseStatus.NotFound,
                    "VerificationNotFoundOrProcessed"
                );
            }

            verification.Status = HostVerificationStatus.Rejected;

            _verificationRepo.Update(verification);
            await _verificationRepo.SaveAsync();

            if (!string.IsNullOrWhiteSpace(reason))
            {
                await _notificationService.SendNotificationAsync(
                    verification.UserId,
                    "HostVerificationRejectedWithReason",
                    new[] { reason },
                    "/Account/BecomeAHost"
                );
            }
            else
            {
                await _notificationService.SendNotificationAsync(
                    verification.UserId,
                    "HostVerificationRejectedNotification",
                    null,
                    "/Account/BecomeAHost"
                );
            }

            return Response<bool>.Success(
                true,
                "HostApplicationRejected"
            );
        }


        // =========================================================
        // USERS
        // =========================================================

        public async Task<Response<AdminUsersViewModel>> GetAllUsersAsync()
        {
            var users = await _userManager.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            var vm = new AdminUsersViewModel
            {
                Users = users.Select(u => new UserAdminRowViewModel
                {
                    Id = u.Id,
                    FullName = $"{u.FirstName} {u.LastName}".Trim(),
                    Email = u.Email ?? string.Empty,
                    IsHost = u.IsHost,
                    IsLockedOut =
                        u.LockoutEnd.HasValue &&
                        u.LockoutEnd.Value > DateTimeOffset.UtcNow,
                    CreatedAt = u.CreatedAt
                }).ToList()
            };

            return Response<AdminUsersViewModel>.Success(vm);
        }


        public async Task<Response<bool>> LockUserAsync(
            int userId,
            DateTimeOffset lockoutEnd)
        {
            var user = await _userManager.FindByIdAsync(
                userId.ToString()
            );

            if (user == null)
            {
                return Response<bool>.Fail(
                    ResponseStatus.NotFound,
                    "UserNotFound"
                );
            }

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return Response<bool>.Fail(
                    ResponseStatus.Unauthorized,
                    "CannotLockAdmin"
                );
            }

            var lockResult =
                await _userManager.SetLockoutEndDateAsync(
                    user,
                    lockoutEnd
                );

            if (!lockResult.Succeeded)
            {
                return Response<bool>.Fail(
                    ResponseStatus.Error,
                    "FailedToLockUser",
                    lockResult.Errors
                        .Select(e => e.Description)
                        .ToList()
                );
            }

            var activeTokens = await _tokenRepo.GetAllAsync(
                selector: rt => rt,
                filter: rt =>
                    rt.UserId == userId &&
                    rt.RevokedAt == null
            );

            foreach (var token in activeTokens)
            {
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedByIp = "Admin-Action";
                _tokenRepo.Update(token);
            }

            await _tokenRepo.SaveAsync();

            return Response<bool>.Success(
                true,
                "UserLockedSessionsRevoked"
            );
        }


        public async Task<Response<bool>> UnlockUserAsync(int userId)
        {
            var user = await _userManager.FindByIdAsync(
                userId.ToString()
            );

            if (user == null)
            {
                return Response<bool>.Fail(
                    ResponseStatus.NotFound,
                    "UserNotFound"
                );
            }

            var unlockResult =
                await _userManager.SetLockoutEndDateAsync(
                    user,
                    null
                );

            if (!unlockResult.Succeeded)
            {
                return Response<bool>.Fail(
                    ResponseStatus.Error,
                    "FailedToUnlockUser",
                    unlockResult.Errors
                        .Select(e => e.Description)
                        .ToList()
                );
            }

            return Response<bool>.Success(
                true,
                "UserUnlocked"
            );
        }


        // =========================================================
        // LISTINGS
        // =========================================================

        public async Task<Response<AdminListingsViewModel>>
            GetAllListingsForModerationAsync()
        {
            var listings = await _listingRepo.GetAllAsync(
                selector: l => l,
                orderBy: q => q.OrderByDescending(l => l.CreatedAt),
                Includes: l => l.Host
            );

            var vm = new AdminListingsViewModel
            {
                Listings = listings.Select(l => new ListingAdminRowViewModel
                {
                    Id = l.Id,
                    Title = l.Title,
                    HostName =
                        $"{l.Host?.FirstName} {l.Host?.LastName}".Trim(),
                    City = l.City,
                    IsActive = l.IsActive,
                    CreatedAt = l.CreatedAt
                }).ToList()
            };

            return Response<AdminListingsViewModel>.Success(vm);
        }


        public async Task<Response<bool>> ModerateListingAsync(
            int listingId,
            bool isActive)
        {
            var listing = await _listingRepo.GetAsync(
                selector: l => l,
                filter: l => l.Id == listingId,
                Includes: l => l.Host
            );

            if (listing == null)
            {
                return Response<bool>.Fail(
                    ResponseStatus.NotFound,
                    "ListingNotFound"
                );
            }

            listing.IsActive = isActive;

            _listingRepo.Update(listing);
            await _listingRepo.SaveAsync();

            if (!isActive)
            {
                await _notificationService.SendNotificationAsync(
    listing.HostId,
    "ListingDeactivatedNotification",
    new[] { listing.Title },
    "/Listings/MyListings"
);
            }

            return Response<bool>.Success(
                true,
                "ListingModerationUpdated"
            );
        }


        // =========================================================
        // DASHBOARD
        // =========================================================

        public async Task<Response<AdminDashboardViewModel>>
            GetDashboardStatsAsync()
        {
            var totalUsers =
                await _userManager.Users.CountAsync();

            var totalListings =
                await _listingRepo.Count();

            var totalBookings =
                await _bookingRepo.Count();

            var totalRevenue =
                await _paymentRepo
                    .GetAllAsIQueryable()
                    .Where(p => p.Status == PaymentStatus.Success)
                    .SumAsync(p => p.Amount);


            var recentUsers =
                await _userManager.Users
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(10)
                    .Select(u => new RecentActivityViewModel
                    {
                        Type = RecentActivityType.NewUserRegistered,
                        CreatedAt = u.CreatedAt,
                        Args = new object[] { u.FirstName, u.LastName }
                    })
                    .ToListAsync();

            var recentListings =
                await _listingRepo
                    .GetAllAsIQueryable()
                    .OrderByDescending(l => l.CreatedAt)
                    .Take(10)
                    .Select(l => new RecentActivityViewModel
                    {
                        Type = RecentActivityType.NewListingCreated,
                        CreatedAt = l.CreatedAt,
                        Args = new object[] { l.Title }
                    })
                    .ToListAsync();

            var recentBookings =
                await _bookingRepo
                    .GetAllAsIQueryable()
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(10)
                    .Select(b => new RecentActivityViewModel
                    {
                        Type = RecentActivityType.NewBookingMade,
                        CreatedAt = b.CreatedAt,
                        Args = new object[] { b.ListingId }
                    })
                    .ToListAsync();

            var recentActivity =
                recentUsers
                    .Concat(recentListings)
                    .Concat(recentBookings)
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(10)

                    .ToList();
            var vm = new AdminDashboardViewModel
            {
                TotalUsers = totalUsers,
                TotalListings = totalListings,
                TotalBookings = totalBookings,
                TotalRevenue = totalRevenue,
                RecentActivity = recentActivity
            };

            return Response<AdminDashboardViewModel>.Success(vm);
        }
    }
}