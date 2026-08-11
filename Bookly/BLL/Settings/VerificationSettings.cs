namespace BLL.Settings
{
    public class VerificationSettings
    {
        public string[] AllowedExtensions { get; set; } = Array.Empty<string>();
        public int MaxFileSizeMb { get; set; }
    }
}