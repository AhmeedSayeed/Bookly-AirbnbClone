using System;
using System.Collections.Generic;

namespace BLL.DTOs.Elasticsearch
{
    public class ListingDocument
    {
        // Identification
        public int Id { get; set; }
        
        // Text fields for Full-Text Search
        public string Title { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        
        // Data for Card Display
        public decimal PricePerNight { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string HostName { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        
        // Exact numbers for Range Queries
        public int MaxGuests { get; set; }
        public int Bedrooms { get; set; }
        public int Bathrooms { get; set; }
        
        // Categorical Exact Matches
        public string PropertyType { get; set; } = string.Empty;
        public string CancellationPolicy { get; set; } = string.Empty;
        
        // Arrays for Term Queries
        public List<int> AmenityIds { get; set; } = new List<int>();
        
        // Flattened Dates for Availability Checking (MustNot match these dates)
        public List<DateTime> UnavailableDates { get; set; } = new List<DateTime>();
        
        public DateTime CreatedAt { get; set; }
    }
}