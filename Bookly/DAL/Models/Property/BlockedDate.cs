using System;

namespace DAL.Models.Property
{
    public class BlockedDate
    {
        public int Id { get; set; }
        
        // Foreign Key
        public int ListingId { get; set; }
        
        public DateTime Date { get; set; }
        public string? Reason { get; set; }

        // Navigation Property
        public Listing Listing { get; set; } = null!;
    }
}