using FluentValidation;

namespace Accounting.Application.Queries.TmpVoucherHeads.GetTmpVoucherHeadById;

/// <summary>
/// Surface-level (syntactic) validation only — rejects an empty <see cref="Guid"/>.
/// </summary>
public sealed class GetTmpVoucherHeadByIdQueryValidator : AbstractValidator<GetTmpVoucherHeadByIdQuery>
{
    public GetTmpVoucherHeadByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);
    }
}
