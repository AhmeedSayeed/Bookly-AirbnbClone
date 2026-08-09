using System;
using System.Collections.Generic;
using DAL.Enums;
using DAL.Models.Identity;
using DAL.Models.Interactions;
using DAL.Models.Property;

namespace DAL.Models.Reservations
{
    public class Booking
    {
        public int Id { get; set; }
        
        // Foreign Keys
        public int ListingId { get; set; }
        public int GuestId { get; set; }
        
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int NumberOfGuests { get; set; }
        public decimal TotalPrice { get; set; }
        public BookingStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        public Listing Listing { get; set; } = null!;
        public ApplicationUser Guest { get; set; } = null!;
        
        // 1-to-1 relationships
        public Review? Review { get; set; }
        public Payment? Payment { get; set; }
        
        // 1-to-many relationship
        public ICollection<Message> Messages { get; set; } = new HashSet<Message>();
    }
}