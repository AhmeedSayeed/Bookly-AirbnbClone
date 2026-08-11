using BLL.DTOs;
using BLL.Services.Interfaces;
using BLL.Settings;
using BLL.ViewModels.Account;
using DAL.Enums;
using DAL.Models.Identity;
using DAL.Repository.Interfaces;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Services.Implementation
{
    public class VerificationService : IVerificationService
    {
        private readonly IRepository<HostVerification> _verificationRepo;
        private readonly IFileUploader _fileUploader;
        private readonly VerificationSettings _settings;

        public VerificationService(
            IRepository<HostVerification> verificationRepo,
            IFileUploader fileUploader,
            IOptions<VerificationSettings> settings)
        {
            _verificationRepo = verificationRepo;
            _fileUploader = fileUploader;
            _settings = settings.Value;
        }

        public async Task<Response<HostVerification>> GetVerificationByUserIdAsync(int userId)
        {
            var verification = await _verificationRepo.GetAsync(
                selector: v => v,
                filter: v => v.UserId == userId);

            return Response<HostVerification>.Success(verification);
        }

        public async Task<Response<bool>> SubmitVerificationAsync(int userId, BecomeAHostViewModel model)
        {
            var extension = Path.GetExtension(model.IdDocument.FileName).ToLowerInvariant();
            if (!_settings.AllowedExtensions.Contains(extension))
            {
                return Response<bool>.Fail(ResponseStatus.ValidationError,
                    $"Invalid file type. Allowed extensions are: {string.Join(", ", _settings.AllowedExtensions)}");
            }

            var maxBytes = _settings.MaxFileSizeMb * 1024 * 1024;
            if (model.IdDocument.Length > maxBytes)
            {
                return Response<bool>.Fail(ResponseStatus.ValidationError,
                    $"File is too large. Maximum allowed size is {_settings.MaxFileSizeMb}MB.");
            }

            var existingResponse = await GetVerificationByUserIdAsync(userId);
            var existing = existingResponse.Data;

            if (existing != null && existing.Status != HostVerificationStatus.Rejected)
            {
                return Response<bool>.Fail(ResponseStatus.Conflict, "You already have a pending or verified application.");
            }

            var documentUrl = await _fileUploader.SaveFileAsync(model.IdDocument, "verifications");

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
                ? Response<bool>.Success(true, "Verification submitted successfully.")
                : Response<bool>.Fail(ResponseStatus.Error, "Failed to submit verification.");
        }
    }
}