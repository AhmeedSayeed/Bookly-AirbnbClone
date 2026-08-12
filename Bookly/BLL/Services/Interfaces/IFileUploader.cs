using BLL.DTOs;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Services.Interfaces
{
    public interface IFileUploader
    {
        public Task<Response<string>> SaveFileAsync(IFormFile file, string subFolder, bool isImage);
        public void DeleteFile(string relativeUrl);
    }
}
