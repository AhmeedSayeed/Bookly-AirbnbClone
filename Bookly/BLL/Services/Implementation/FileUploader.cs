using BLL.DTOs;
using BLL.Services.Interfaces;
using BLL.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BLL.Services.Implementation
{
    public class FileUploader : IFileUploader
    {
        private readonly FileStorageSettings _settings;
        private readonly IWebHostEnvironment _env;

        public FileUploader(
            IOptions<FileStorageSettings> settings,
            IWebHostEnvironment env)
        {
            _settings = settings.Value;
            _env = env;
        }

        public async Task<Response<string>> SaveFileAsync(
            IFormFile file,
            string subFolder,
            bool isImage = false)
        {
            if (file is null || file.Length == 0)
            {
                return Response<string>.FailWithKey(
                    ResponseStatus.ValidationError,
                    "NoFileProvided");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (isImage)
            {
                if (!_settings.AllowedImageExtensions.Contains(extension))
                {
                    return Response<string>.FailWithKey(
                        ResponseStatus.ValidationError,
                        "ImageTypeNotAllowed",
                        new[] { extension });
                }
            }
            else
            {
                if (!_settings.AllowedFileExtensions.Contains(extension))
                {
                    return Response<string>.FailWithKey(
                        ResponseStatus.ValidationError,
                        "FileTypeNotAllowed",
                        new[] { extension });
                }
            }

            var maxBytes = _settings.MaxFileSizeMB * 1024 * 1024;

            if (file.Length > maxBytes)
            {
                return Response<string>.FailWithKey(
                    ResponseStatus.ValidationError,
                    "FileExceedsSizeLimit",
                    new[] { _settings.MaxFileSizeMB.ToString() });
            }

            var fileName = $"{Guid.NewGuid()}{extension}";
            var folderPath = Path.Combine(
                _env.WebRootPath,
                "uploads",
                subFolder);

            Directory.CreateDirectory(folderPath);

            var fullPath = Path.Combine(folderPath, fileName);

            await using var stream = new FileStream(
                fullPath,
                FileMode.Create);

            await file.CopyToAsync(stream);

            return Response<string>.Success(
                $"/uploads/{subFolder}/{fileName}");
        }

        public void DeleteFile(string relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl))
                return;

            var relativePath = relativeUrl
                .TrimStart('/')
                .Replace('/', Path.DirectorySeparatorChar);

            var fullPath = Path.Combine(
                _env.WebRootPath,
                relativePath);

            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
    }
}