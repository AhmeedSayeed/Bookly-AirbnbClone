using FluentValidation;
using BLL.ViewModels.Listings;

namespace BLL.Validators;

public class ListingFormViewModelValidator : AbstractValidator<ListingFormViewModel>
{
    private static readonly string[] AllowedPropertyTypes =
        { "Apartment", "House", "Room", "Villa", "Studio", "Other" };

    private static readonly string[] AllowedCancellationPolicies =
        { "Flexible", "Moderate", "Strict" };

    private static readonly string[] AllowedPhotoTypes =
        { "image/jpeg", "image/png", "image/webp" };

    public ListingFormViewModelValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);

        RuleFor(x => x.PropertyType)
            .NotEmpty()
            .Must(t => AllowedPropertyTypes.Contains(t))
            .WithMessage($"Property type must be one of: {string.Join(", ", AllowedPropertyTypes)}.");

        RuleFor(x => x.Address).NotEmpty();
        RuleFor(x => x.City).NotEmpty();
        RuleFor(x => x.Country).NotEmpty();

        RuleFor(x => x.Latitude).InclusiveBetween(-90m, 90m).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180m, 180m).When(x => x.Longitude.HasValue);

        RuleFor(x => x.PricePerNight).GreaterThan(0).LessThanOrEqualTo(100000);
        RuleFor(x => x.MaxGuests).InclusiveBetween(1, 50);
        RuleFor(x => x.Bedrooms).InclusiveBetween(0, 20);
        RuleFor(x => x.Bathrooms).InclusiveBetween(0, 20);
        RuleFor(x => x.Beds).InclusiveBetween(0, 20);

        RuleFor(x => x.CancellationPolicy)
            .Must(p => p == null || AllowedCancellationPolicies.Contains(p))
            .WithMessage($"Cancellation policy must be one of: {string.Join(", ", AllowedCancellationPolicies)}.");

        RuleFor(x => x.SelectedAmenityIds)
            .Must(ids => ids == null || ids.Distinct().Count() == ids.Count)
            .WithMessage("Duplicate amenities selected.");

        RuleFor(x => x.NewPhotos)
            .Must(files => files == null || files.All(f => f.Length <= 5 * 1024 * 1024))
            .WithMessage("Each photo must be 5MB or smaller.")
            .Must(files => files == null || files.All(f => AllowedPhotoTypes.Contains(f.ContentType)))
            .WithMessage("Photos must be JPEG, PNG, or WebP.");
    }
}
