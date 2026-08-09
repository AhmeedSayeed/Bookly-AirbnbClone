namespace DAL.Models.Property
{
    public class ListingAmenity
    {
        // Id is Composite PK (ListingId, AmenityId)

        // Foreign Keys
        public int ListingId { get; set; }
        public int AmenityId { get; set; }

        // Navigation Properties
        public Listing Listing { get; set; } = null!;
        public Amenity Amenity { get; set; } = null!;
    }
}