using FluentValidation;
using BLL.ViewModels.Listings;

namespace BLL.Validators;

public class ListingFormViewModelValidator : AbstractValidator<ListingFormViewModel>
{
    private static readonly string[] AllowedPhotoTypes =
        { "image/jpeg", "image/png", "image/webp" };

    public ListingFormViewModelValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("ListingTitleRequired")
            .MaximumLength(120)
            .WithMessage("ListingTitleMaximumLength");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("ListingDescriptionRequired")
            .MaximumLength(2000)
            .WithMessage("ListingDescriptionMaximumLength");

        RuleFor(x => x.PropertyType)
            .IsInEnum()
            .WithMessage("InvalidPropertyTypeSelected");

        RuleFor(x => x.Address)
            .NotEmpty()
            .WithMessage("AddressRequired");

        RuleFor(x => x.City)
            .NotEmpty()
            .WithMessage("CityRequired");

        RuleFor(x => x.Country)
            .NotEmpty()
            .WithMessage("CountryRequired");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90m, 90m)
            .When(x => x.Latitude.HasValue)
            .WithMessage("LatitudeMustBeBetweenMinus90And90");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180m, 180m)
            .When(x => x.Longitude.HasValue)
            .WithMessage("LongitudeMustBeBetweenMinus180And180");

        RuleFor(x => x.PricePerNight)
            .GreaterThan(0)
            .WithMessage("PricePerNightMustBeGreaterThanZero")
            .LessThanOrEqualTo(100000)
            .WithMessage("PricePerNightMaximum");

        RuleFor(x => x.MaxGuests)
            .InclusiveBetween(1, 50)
            .WithMessage("MaxGuestsMustBeBetween1And50");

        RuleFor(x => x.Bedrooms)
            .InclusiveBetween(0, 20)
            .WithMessage("BedroomsMustBeBetween0And20");

        RuleFor(x => x.Bathrooms)
            .InclusiveBetween(0, 20)
            .WithMessage("BathroomsMustBeBetween0And20");

        RuleFor(x => x.Beds)
            .InclusiveBetween(0, 20)
            .WithMessage("BedsMustBeBetween0And20");

        RuleFor(x => x.CancellationPolicy)
            .IsInEnum()
            .WithMessage("InvalidCancellationPolicySelected");

        RuleFor(x => x.SelectedAmenityIds)
            .Must(ids => ids == null || ids.Distinct().Count() == ids.Count)
            .WithMessage("DuplicateAmenitiesSelected");

        RuleFor(x => x.NewPhotos)
            .Must(files => files == null || files.All(f => f.Length <= 5 * 1024 * 1024))
            .WithMessage("PhotoMaximumSize");

        RuleFor(x => x.NewPhotos)
            .Must(files => files == null || files.All(f => AllowedPhotoTypes.Contains(f.ContentType)))
            .WithMessage("PhotoTypesNotAllowed");
    }
}