using DAL.Models.Identity;
using System;

namespace DAL.Models.Interactions
{
    public class Notification
    {
        public int Id { get; set; }
        
        // Foreign Key
        public int UserId { get; set; }
        
        public string Message { get; set; } = string.Empty;
        public string? Link { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation Property
        public ApplicationUser User { get; set; } = null!;
    }
}