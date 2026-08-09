using DAL.Models.Interactions;
using DAL.Models.Property;
using DAL.Models.Reservations;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace DAL.Models.Identity
{
    public class ApplicationUser : IdentityUser<int>
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? ProfilePhotoUrl { get; set; }
        public bool IsHost { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new HashSet<RefreshToken>();
        public ICollection<Listing> Listings { get; set; } = new HashSet<Listing>();
        public ICollection<Booking> Bookings { get; set; } = new HashSet<Booking>();
        public ICollection<Wishlist> Wishlists { get; set; } = new HashSet<Wishlist>();
        public ICollection<Message> SentMessages { get; set; } = new HashSet<Message>();
        public ICollection<Message> ReceivedMessages { get; set; } = new HashSet<Message>();
        public ICollection<Notification> Notifications { get; set; } = new HashSet<Notification>();

        // 1-to-1 relationship
        public HostVerification? HostVerification { get; set; }
    }
}