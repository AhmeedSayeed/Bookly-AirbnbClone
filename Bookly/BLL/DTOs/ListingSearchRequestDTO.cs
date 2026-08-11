using System;
using System.Collections.Generic;

namespace BLL.DTOs.Listing
{
    public class ListingSearchRequestDto
    {
        public string? City { get; set; }
        public int? Guests { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public List<int> AmenityIds { get; set; } = new();
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}