using System;
using System.Collections.Generic;

namespace BLL.DTOs.Listing
{
    public class ListingSearchRequestDto
    {
        // Main search bar (Omnibox) for City, Title, or keywords
        public string? SearchTerm { get; set; }

        // Exact Price Range (Slider)
        public int? MinPrice { get; set; }
        public int? MaxPrice { get; set; }

        // Categorical Ranges (e.g., "1-3", "4-5", "6-10", "10+")
        public string? GuestsRange { get; set; }
        public string? BedroomsRange { get; set; }
        public string? BathroomsRange { get; set; }

        // Checkbox Filters
        public List<int>? AmenityIds { get; set; }
        public List<string>? PropertyTypes { get; set; }
        public List<string>? CancellationPolicies { get; set; }

        // Date Availability
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }

        // Pagination
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}