using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace BLL.DTOs.Listing
{
    public class ListingCreateDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal PricePerNight { get; set; }
        public int MaxGuests { get; set; }

        public List<int> SelectedAmenityIds { get; set; } = new();

        public List<IFormFile> Photos { get; set; } = new();
    }

    public class ListingUpdateDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal PricePerNight { get; set; }
        public bool IsActive { get; set; }

        public List<int> SelectedAmenityIds { get; set; } = new();
    }
}