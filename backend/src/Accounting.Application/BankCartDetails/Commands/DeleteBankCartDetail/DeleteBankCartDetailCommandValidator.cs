using FluentValidation;

namespace Accounting.Application.BankCartDetails.Commands.DeleteBankCartDetail;

/// <summary>
/// Surface-level validation only — the single field is the route-bound <c>Id</c>.
/// </summary>
public sealed class DeleteBankCartDetailCommandValidator : AbstractValidator<DeleteBankCartDetailCommand>
{
    public DeleteBankCartDetailCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
