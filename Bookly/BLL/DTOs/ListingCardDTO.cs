namespace BLL.DTOs.Listing
{
    public class ListingCardDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public decimal PricePerNight { get; set; }

        public string? ThumbnailUrl { get; set; }

        public string HostName { get; set; } = string.Empty;

        // Whether the currently logged-in user has saved this listing to their wishlist.
        // Not set by the DB query itself - populated afterwards by the controller.
        public bool IsWishlisted { get; set; }
    }
}