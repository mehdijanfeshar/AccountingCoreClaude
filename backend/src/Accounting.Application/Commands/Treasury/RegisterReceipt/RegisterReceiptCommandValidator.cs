using FluentValidation;

namespace Accounting.Application.Commands.Treasury.RegisterReceipt;

public sealed class RegisterReceiptCommandValidator : AbstractValidator<RegisterReceiptCommand>
{
    public RegisterReceiptCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
