using FluentValidation;
using BLL.ViewModels.Common;

namespace BLL.Validators;

public class SearchFilterViewModelValidator : AbstractValidator<SearchFilterViewModel>
{
    public SearchFilterViewModelValidator()
    {
        RuleFor(x => x.Guests).GreaterThanOrEqualTo(1);

        RuleFor(x => x.CheckIn)
            .GreaterThanOrEqualTo(DateTime.Today)
            .When(x => x.CheckIn.HasValue)
            .WithMessage("Check-in date can't be in the past.");

        RuleFor(x => x.CheckOut)
            .GreaterThan(x => x.CheckIn!.Value)
            .When(x => x.CheckIn.HasValue && x.CheckOut.HasValue)
            .WithMessage("Check-out date must be after check-in date.");

        RuleFor(x => x.MaxPrice)
            .GreaterThanOrEqualTo(x => x.MinPrice!.Value)
            .When(x => x.MinPrice.HasValue && x.MaxPrice.HasValue)
            .WithMessage("Maximum price must be greater than or equal to minimum price.");
    }
}
