using FluentValidation;
using BLL.ViewModels.Messages;

namespace BLL.Validators;

public class MessageFormViewModelValidator : AbstractValidator<MessageFormViewModel>
{
    public MessageFormViewModelValidator()
    {
        RuleFor(x => x.ReceiverId)
            .GreaterThan(0)
            .WithMessage("ReceiverIdMustBeGreaterThanZero");

        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("MessageContentRequired")
            .MaximumLength(2000)
            .WithMessage("MessageContentMaximumLength");
    }
}