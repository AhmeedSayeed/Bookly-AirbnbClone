using FluentValidation;
using BLL.ViewModels.Bookings;

namespace BLL.Validators;

public class BookingRequestViewModelValidator : AbstractValidator<BookingRequestViewModel>
{
    public BookingRequestViewModelValidator()
    {
        RuleFor(x => x.ListingId)
            .GreaterThan(0)
            .WithMessage("ListingIdMustBeGreaterThanZero");

        RuleFor(x => x.CheckInDate)
            .GreaterThanOrEqualTo(DateTime.Today)
            .WithMessage("CheckInDateCannotBeInPast");

        RuleFor(x => x.CheckOutDate)
            .GreaterThan(x => x.CheckInDate)
            .WithMessage("CheckOutDateMustBeAfterCheckIn");

        RuleFor(x => x)
            .Must(x => (x.CheckOutDate - x.CheckInDate).TotalDays <= 90)
            .WithMessage("BookingsCannotExceed90Nights")
            .WithName(nameof(BookingRequestViewModel.CheckOutDate));

        RuleFor(x => x.NumberOfGuests)
            .InclusiveBetween(1, 50)
            .WithMessage("NumberOfGuestsMustBeBetween1And50");
    }
}