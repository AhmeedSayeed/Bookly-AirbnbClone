using FluentValidation;
using BLL.ViewModels.Availability;

namespace BLL.Validators;

public class AvailabilityCalendarViewModelValidator
    : AbstractValidator<AvailabilityCalendarViewModel>
{
    public AvailabilityCalendarViewModelValidator()
    {
        RuleFor(x => x.ListingId)
            .GreaterThan(0)
            .WithMessage("ListingIdMustBeGreaterThanZero");

        RuleForEach(x => x.BlockedDates)
            .GreaterThanOrEqualTo(DateTime.Today)
            .WithMessage("BlockedDatesCannotBeInPast");

        RuleFor(x => x.BlockedDates)
            .Must(dates => dates.Distinct().Count() == dates.Count)
            .WithMessage("DuplicateBlockedDates");
    }
}