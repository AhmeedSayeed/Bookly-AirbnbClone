using FluentValidation;
using BLL.ViewModels.Bookings;

namespace BLL.Validators;

public class BookingRequestViewModelValidator : AbstractValidator<BookingRequestViewModel>
{
    public BookingRequestViewModelValidator()
    {
        RuleFor(x => x.ListingId).GreaterThan(0);

        RuleFor(x => x.CheckInDate)
            .GreaterThanOrEqualTo(DateTime.Today)
            .WithMessage("Check-in date can't be in the past.");

        RuleFor(x => x.CheckOutDate)
            .GreaterThan(x => x.CheckInDate)
            .WithMessage("Check-out date must be after check-in date.");

        RuleFor(x => x)
            .Must(x => (x.CheckOutDate - x.CheckInDate).TotalDays <= 90)
            .WithMessage("Bookings can't span more than 90 nights.")
            .WithName(nameof(BookingRequestViewModel.CheckOutDate));

        RuleFor(x => x.NumberOfGuests).InclusiveBetween(1, 50);
    }
}
