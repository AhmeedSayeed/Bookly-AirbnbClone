using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Services.Interfaces
{
    public interface IFileUploader
    {
        public Task<string> SaveFileAsync(IFormFile file, string subFolder);
        public void DeleteFile(string relativeUrl);
    }
}
