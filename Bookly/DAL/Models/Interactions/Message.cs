using DAL.Models.Common;
using DAL.Models.Identity;
using DAL.Models.Reservations;
using System;

namespace DAL.Models.Interactions
{
    public class Message : ISoftDeletable
    {
        public int Id { get; set; }

        // Foreign Keys
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public int? BookingId { get; set; }

        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation Properties
        public ApplicationUser Sender { get; set; } = null!;
        public ApplicationUser Receiver { get; set; } = null!;
        public Booking? Booking { get; set; }
    }
}