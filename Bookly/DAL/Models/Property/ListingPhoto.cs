namespace DAL.Models.Property
{
    public class ListingPhoto
    {
        public int Id { get; set; }
        
        // Foreign Key
        public int ListingId { get; set; }
        
        public string Url { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }

        // Navigation Property
        public Listing Listing { get; set; } = null!;
    }
}