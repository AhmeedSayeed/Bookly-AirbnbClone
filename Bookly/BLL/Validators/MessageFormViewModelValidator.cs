using FluentValidation;
using BLL.ViewModels.Messages;

namespace BLL.Validators;

public class MessageFormViewModelValidator : AbstractValidator<MessageFormViewModel>
{
    public MessageFormViewModelValidator()
    {
        RuleFor(x => x.ReceiverId).GreaterThan(0);
        RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);
    }
}
