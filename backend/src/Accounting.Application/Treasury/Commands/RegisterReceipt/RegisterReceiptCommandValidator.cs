using FluentValidation;

namespace Accounting.Application.Treasury.Commands.RegisterReceipt;

public sealed class RegisterReceiptCommandValidator : AbstractValidator<RegisterReceiptCommand>
{
    public RegisterReceiptCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
