using FluentValidation;

namespace Accounting.Application.Receipts.Commands.DeleteReceipt;

/// <summary>
/// Surface-level validation only — the single field is the route-bound <c>Id</c>.
/// </summary>
public sealed class DeleteReceiptCommandValidator : AbstractValidator<DeleteReceiptCommand>
{
    public DeleteReceiptCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
