using FluentValidation;

namespace Accounting.Application.Queries.BillLogs.GetBillLogById;

/// <summary>
/// Surface-level (syntactic) validation only — rejects an empty <see cref="Guid"/>.
/// </summary>
public sealed class GetBillLogByIdQueryValidator : AbstractValidator<GetBillLogByIdQuery>
{
    public GetBillLogByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);
    }
}
