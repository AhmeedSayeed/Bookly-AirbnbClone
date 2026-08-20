using DAL.Enums;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BLL.ViewModels.Listings;

public class ExistingListingPhoto
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
}

public class ListingFormViewModel
{
    public int? Id { get; set; }

    [Required, MaxLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public PropertyType PropertyType { get; set; }

    [Required]
    public string Address { get; set; } = string.Empty;

    [Required]
    public string City { get; set; } = string.Empty;

    [Required]
    public string Country { get; set; } = string.Empty;

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    [Required, Range(1, 100000)]
    public decimal PricePerNight { get; set; }

    [Required, Range(1, 50)]
    public int MaxGuests { get; set; }

    [Required, Range(0, 20)]
    public int Bedrooms { get; set; }

    [Required, Range(0, 20)]
    public int Bathrooms { get; set; }

    [Required, Range(0, 20)]
    public int Beds { get; set; }

    public CancellationPolicy CancellationPolicy { get; set; }

    public List<int> SelectedAmenityIds { get; set; } = new();
    public List<IFormFile>? NewPhotos { get; set; }

    public List<ExistingListingPhoto> ExistingPhotos { get; set; } = new();
    public List<int> DeletedPhotoIds { get; set; } = new();
}