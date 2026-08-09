using DAL.Models.Identity;
using DAL.Models.Property;

namespace DAL.Models.Interactions
{
    public class Wishlist
    {
        public int Id { get; set; }
        
        // Foreign Keys
        public int UserId { get; set; }
        public int ListingId { get; set; }

        // Navigation Properties
        public ApplicationUser User { get; set; } = null!;
        public Listing Listing { get; set; } = null!;
    }
}