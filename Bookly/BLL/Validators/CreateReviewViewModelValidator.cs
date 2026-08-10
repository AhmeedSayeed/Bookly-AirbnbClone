using FluentValidation;
using BLL.ViewModels.Reviews;

namespace BLL.Validators;

public class CreateReviewViewModelValidator : AbstractValidator<CreateReviewViewModel>
{
    public CreateReviewViewModelValidator()
    {
        RuleFor(x => x.BookingId).GreaterThan(0);
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Comment).MaximumLength(1000);

        RuleFor(x => x.CleanlinessRating).InclusiveBetween(1, 5).When(x => x.CleanlinessRating.HasValue);
        RuleFor(x => x.CommunicationRating).InclusiveBetween(1, 5).When(x => x.CommunicationRating.HasValue);
        RuleFor(x => x.LocationRating).InclusiveBetween(1, 5).When(x => x.LocationRating.HasValue);
        RuleFor(x => x.ValueRating).InclusiveBetween(1, 5).When(x => x.ValueRating.HasValue);
    }
}
