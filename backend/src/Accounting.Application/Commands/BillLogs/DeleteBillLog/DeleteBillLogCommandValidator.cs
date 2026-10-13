using FluentValidation;

namespace Accounting.Application.Commands.BillLogs.DeleteBillLog;

/// <summary>
/// Surface-level validation only — the single field is the route-bound <c>Id</c>.
/// </summary>
public sealed class DeleteBillLogCommandValidator : AbstractValidator<DeleteBillLogCommand>
{
    public DeleteBillLogCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
