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

        public VerificationService(
            IRepository<HostVerification> verificationRepo,
            IFileUploader fileUploader)
        {
            _verificationRepo = verificationRepo;
            _fileUploader = fileUploader;
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

            return saved > 0
                ? Response<bool>.SuccessWithKey(
                    true,
                    "VerificationSubmittedSuccessfully")
                : Response<bool>.FailWithKey(
                    ResponseStatus.Error,
                    "FailedToSubmitVerification");
        }
    }
}