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

    [Required(ErrorMessage = "ListingTitleRequired")]
    [MaxLength(120, ErrorMessage = "ListingTitleMaximumLength")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "ListingDescriptionRequired")]
    [MaxLength(2000, ErrorMessage = "ListingDescriptionMaximumLength")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "PropertyTypeRequired")]
    public PropertyType PropertyType { get; set; }

    [Required(ErrorMessage = "AddressRequired")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "CityRequired")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "CountryRequired")]
    public string Country { get; set; } = string.Empty;

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    [Required(ErrorMessage = "PricePerNightRequired")]
    [Range(1, 100000, ErrorMessage = "PricePerNightRange")]
    public decimal PricePerNight { get; set; }

    [Required(ErrorMessage = "MaxGuestsRequired")]
    [Range(1, 50, ErrorMessage = "MaxGuestsRange")]
    public int MaxGuests { get; set; }

    [Required(ErrorMessage = "BedroomsRequired")]
    [Range(0, 20, ErrorMessage = "BedroomsRange")]
    public int Bedrooms { get; set; }

    [Required(ErrorMessage = "BathroomsRequired")]
    [Range(0, 20, ErrorMessage = "BathroomsRange")]
    public int Bathrooms { get; set; }

    [Required(ErrorMessage = "BedsRequired")]
    [Range(0, 20, ErrorMessage = "BedsRange")]
    public int Beds { get; set; }

    [Required(ErrorMessage = "CancellationPolicyRequired")]
    public CancellationPolicy CancellationPolicy { get; set; }

    public List<int> SelectedAmenityIds { get; set; } = new();
    public List<IFormFile>? NewPhotos { get; set; }

    public List<ExistingListingPhoto> ExistingPhotos { get; set; } = new();
    public List<int> DeletedPhotoIds { get; set; } = new();
}