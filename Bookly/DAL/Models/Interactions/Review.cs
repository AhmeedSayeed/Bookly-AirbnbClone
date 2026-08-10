using DAL.Models.Common;
using DAL.Models.Reservations;
using System;

namespace DAL.Models.Interactions
{
    public class Review : ISoftDeletable
    {
        public int Id { get; set; }

        // Foreign Key
        public int BookingId { get; set; }

        public int Rating { get; set; }
        public string? Comment { get; set; }
        public int? CleanlinessRating { get; set; }
        public int? CommunicationRating { get; set; }
        public int? LocationRating { get; set; }
        public int? ValueRating { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation Properties
        public Booking Booking { get; set; } = null!;

        // 1-to-1 relationship
        public HostResponse? HostResponse { get; set; }
    }
}