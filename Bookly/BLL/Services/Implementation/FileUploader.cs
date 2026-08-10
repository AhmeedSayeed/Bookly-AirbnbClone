using BLL.Services.Interfaces;
using BLL.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Services.Implementation
{
    public class FileUploader : IFileUploader
    {
        private readonly FileStorageSettings _settings;
        private readonly IWebHostEnvironment _env;

        public FileUploader(IOptions<FileStorageSettings> settings, IWebHostEnvironment env)
        {
            _settings = settings.Value;
            _env = env;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string subFolder)
        {
            if (file is null || file.Length == 0)
                throw new ArgumentException("No file was provided.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_settings.AllowedExtensions.Contains(extension))
                throw new InvalidOperationException($"File type '{extension}' is not allowed.");

            var maxBytes = _settings.MaxFileSizeMB * 1024 * 1024;
            if (file.Length > maxBytes)
                throw new InvalidOperationException($"File exceeds the {_settings.MaxFileSizeMB} MB limit.");

            var fileName = $"{Guid.NewGuid()}{extension}";
            var folderPath = Path.Combine(_env.WebRootPath, "uploads", subFolder);
            Directory.CreateDirectory(folderPath);

            var fullPath = Path.Combine(folderPath, fileName);
            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/uploads/{subFolder}/{fileName}";
        }

        public void DeleteFile(string relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl))
                return;

            var relativePath = relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(_env.WebRootPath, relativePath);

            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
    }
}
