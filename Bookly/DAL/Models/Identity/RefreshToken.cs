using System;

namespace DAL.Models.Identity
{
    public class RefreshToken
    {
        public int Id { get; set; }
        
        // Foreign Key
        public int UserId { get; set; }
        
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedByIp { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? RevokedByIp { get; set; }
        public string? ReplacedByToken { get; set; }

        // Navigation Property
        public ApplicationUser User { get; set; } = null!;
    }
}