using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Settings
{
    public class FileStorageSettings
    {
        public List<string> AllowedImageExtensions { get; set; } = new List<string>();
        public List<string> AllowedFileExtensions { get; set; } = new List<string>();
        public int MaxFileSizeMB { get; set; }
    }
}
