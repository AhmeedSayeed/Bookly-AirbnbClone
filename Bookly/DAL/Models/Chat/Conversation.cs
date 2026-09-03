using DAL.Models.Identity;
using DAL.Models.Property;
using DAL.Models.Reservations;
using System;
using System.Collections.Generic;

namespace DAL.Models.Chat
{
    public class Conversation
    {
        public int Id { get; set; }

        public int ListingId { get; set; }
        public Listing Listing { get; set; }

        public int GuestId { get; set; }
        public ApplicationUser Guest { get; set; }

        public int HostId { get; set; }
        public ApplicationUser Host { get; set; }

        public int? BookingId { get; set; }
        public Booking Booking { get; set; }

        public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}