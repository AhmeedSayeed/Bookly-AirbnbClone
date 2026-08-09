using System.Collections.Generic;

namespace DAL.Models.Property
{
    public class Amenity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? IconClass { get; set; }

        // Navigation Property
        public ICollection<ListingAmenity> ListingAmenities { get; set; } = new HashSet<ListingAmenity>();
    }
}