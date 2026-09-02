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
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public bool IsWishlisted { get; set; }
    }
}