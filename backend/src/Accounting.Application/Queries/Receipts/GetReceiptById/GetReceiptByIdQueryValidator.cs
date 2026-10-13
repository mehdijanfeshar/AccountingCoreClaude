using FluentValidation;

namespace Accounting.Application.Queries.Receipts.GetReceiptById;

/// <summary>
/// Surface-level (syntactic) validation only — rejects an empty <see cref="Guid"/>.
/// </summary>
public sealed class GetReceiptByIdQueryValidator : AbstractValidator<GetReceiptByIdQuery>
{
    public GetReceiptByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);
    }
}
