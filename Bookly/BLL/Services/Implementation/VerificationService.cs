using BLL.DTOs;
using BLL.Services.Interfaces;
using BLL.ViewModels.Account;
using DAL.Enums;
using DAL.Models.Identity;
using DAL.Repository.Interfaces;
using System;
using System.Threading.Tasks;

namespace BLL.Services.Implementation
{
    public class VerificationService : IVerificationService
    {
        private readonly IRepository<HostVerification> _verificationRepo;
        private readonly IFileUploader _fileUploader;
        private readonly INotificationService _notificationService;

        public VerificationService(
            IRepository<HostVerification> verificationRepo,
            IFileUploader fileUploader,
            INotificationService notificationService)
        {
            _verificationRepo = verificationRepo;
            _fileUploader = fileUploader;
            _notificationService = notificationService;
        }

        public async Task<Response<HostVerification>> GetVerificationByUserIdAsync(int userId)
        {
            var verification = await _verificationRepo.GetAsync(
                selector: v => v,
                filter: v => v.UserId == userId);

            return Response<HostVerification>.Success(verification);
        }

        public async Task<Response<bool>> SubmitVerificationAsync(
            int userId,
            BecomeAHostViewModel model)
        {
            var existingResponse = await GetVerificationByUserIdAsync(userId);
            var existing = existingResponse.Data;

            if (existing != null && existing.Status != HostVerificationStatus.Rejected)
            {
                return Response<bool>.FailWithKey(
                    ResponseStatus.Conflict,
                    "VerificationAlreadyPendingOrVerified"
                );
            }

            var uploadResponse = await _fileUploader.SaveFileAsync(
                model.IdDocument,
                "verifications",
                false);

            if (!uploadResponse.Succeeded)
            {
                return Response<bool>.FailWithKey(
                    ResponseStatus.Error,
                    "FileUploadFailed",
                    new[] { uploadResponse.Message }
                );
            }

            var documentUrl = uploadResponse.Data;

            if (existing != null && existing.Status == HostVerificationStatus.Rejected)
            {
                existing.DocumentUrl = documentUrl;
                existing.Status = HostVerificationStatus.Pending;
                existing.SubmittedAt = DateTime.UtcNow;

                _verificationRepo.Update(existing);
            }
            else
            {
                var verification = new HostVerification
                {
                    UserId = userId,
                    DocumentUrl = documentUrl,
                    Status = HostVerificationStatus.Pending,
                    SubmittedAt = DateTime.UtcNow
                };

                await _verificationRepo.AddAsync(verification);
            }

            var saved = await _verificationRepo.SaveAsync();

            if (saved > 0)
            {
                // Notify the user that their submission was received
                await _notificationService.SendNotificationAsync(
                    userId,
                    "VerificationSubmittedNotification",
                    null,
                    "/Account/BecomeAHost"
                );

                return Response<bool>.SuccessWithKey(true, "VerificationSubmittedSuccessfully");
            }

            return Response<bool>.FailWithKey(ResponseStatus.Error, "FailedToSubmitVerification");
        }
    }
}