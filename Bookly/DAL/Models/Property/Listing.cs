using System;
using System.Collections.Generic;
using DAL.Enums;
using DAL.Models.Identity;
using DAL.Models.Interactions;
using DAL.Models.Reservations;

namespace DAL.Models.Property
{
    public class Listing
    {
        public int Id { get; set; }
        
        // Foreign Key
        public int HostId { get; set; }
        
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public PropertyType PropertyType { get; set; }
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public decimal PricePerNight { get; set; }
        public int MaxGuests { get; set; }
        public int Bedrooms { get; set; }
        public int Bathrooms { get; set; }
        public int Beds { get; set; }
        public bool IsActive { get; set; }
        public CancellationPolicy? CancellationPolicy { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        public ApplicationUser Host { get; set; } = null!;
        public ICollection<ListingPhoto> Photos { get; set; } = new HashSet<ListingPhoto>();
        public ICollection<ListingAmenity> ListingAmenities { get; set; } = new HashSet<ListingAmenity>();
        public ICollection<Booking> Bookings { get; set; } = new HashSet<Booking>();
        public ICollection<BlockedDate> BlockedDates { get; set; } = new HashSet<BlockedDate>();
        public ICollection<Wishlist> WishlistedBy { get; set; } = new HashSet<Wishlist>();
    }
}