using System;
using DAL.Enums;

namespace DAL.Models.Identity
{
    public class HostVerification
    {
        public int Id { get; set; }
        
        // Foreign Key
        public int UserId { get; set; }
        
        public string DocumentUrl { get; set; } = string.Empty;
        public HostVerificationStatus Status { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }

        // Navigation Property
        public ApplicationUser User { get; set; } = null!;
    }
}