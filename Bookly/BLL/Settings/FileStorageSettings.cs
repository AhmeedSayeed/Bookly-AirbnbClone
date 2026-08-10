using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Settings
{
    public class FileStorageSettings
    {
        public List<string> AllowedExtensions { get; set; } = new List<string>();
        public int MaxFileSizeMB { get; set; }
    }
}
