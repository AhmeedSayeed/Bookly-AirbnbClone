using FluentValidation;
using BLL.ViewModels.Common;

namespace BLL.Validators;

public class SearchFilterViewModelValidator : AbstractValidator<SearchFilterViewModel>
{
    public SearchFilterViewModelValidator()
    {
        RuleFor(x => x.Guests)
            .GreaterThanOrEqualTo(1)
            .WithMessage("GuestsMustBeAtLeastOne");

        RuleFor(x => x.CheckIn)
            .GreaterThanOrEqualTo(DateTime.Today)
            .When(x => x.CheckIn.HasValue)
            .WithMessage("CheckInDateCannotBeInPast");

        RuleFor(x => x.CheckOut)
            .GreaterThan(x => x.CheckIn!.Value)
            .When(x => x.CheckIn.HasValue && x.CheckOut.HasValue)
            .WithMessage("CheckOutDateMustBeAfterCheckIn");

        RuleFor(x => x.MaxPrice)
            .GreaterThanOrEqualTo(x => x.MinPrice!.Value)
            .When(x => x.MinPrice.HasValue && x.MaxPrice.HasValue)
            .WithMessage("MaximumPriceMustBeGreaterThanOrEqualToMinimumPrice");
    }
}