using FluentValidation;
using BLL.ViewModels.Reviews;

namespace BLL.Validators;

public class CreateReviewViewModelValidator : AbstractValidator<CreateReviewViewModel>
{
    public CreateReviewViewModelValidator()
    {
        RuleFor(x => x.BookingId)
            .GreaterThan(0)
            .WithMessage("BookingIdMustBeGreaterThanZero");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .WithMessage("RatingMustBeBetween1And5");

        RuleFor(x => x.Comment)
            .MaximumLength(1000)
            .WithMessage("ReviewCommentMaximumLength");

        RuleFor(x => x.CleanlinessRating)
            .InclusiveBetween(1, 5)
            .When(x => x.CleanlinessRating.HasValue)
            .WithMessage("RatingMustBeBetween1And5");

        RuleFor(x => x.CommunicationRating)
            .InclusiveBetween(1, 5)
            .When(x => x.CommunicationRating.HasValue)
            .WithMessage("RatingMustBeBetween1And5");

        RuleFor(x => x.LocationRating)
            .InclusiveBetween(1, 5)
            .When(x => x.LocationRating.HasValue)
            .WithMessage("RatingMustBeBetween1And5");

        RuleFor(x => x.ValueRating)
            .InclusiveBetween(1, 5)
            .When(x => x.ValueRating.HasValue)
            .WithMessage("RatingMustBeBetween1And5");
    }
}