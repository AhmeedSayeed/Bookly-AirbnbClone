using FluentValidation;
using BLL.ViewModels.Availability;

namespace BLL.Validators;

public class AvailabilityCalendarViewModelValidator : AbstractValidator<AvailabilityCalendarViewModel>
{
    public AvailabilityCalendarViewModelValidator()
    {
        RuleFor(x => x.ListingId).GreaterThan(0);

        RuleForEach(x => x.BlockedDates)
            .GreaterThanOrEqualTo(DateTime.Today)
            .WithMessage("Blocked dates can't be in the past.");

        RuleFor(x => x.BlockedDates)
            .Must(dates => dates.Distinct().Count() == dates.Count)
            .WithMessage("Duplicate blocked dates.");
    }
}
