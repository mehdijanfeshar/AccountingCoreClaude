using FluentValidation;

namespace Accounting.Application.TmpVoucherHeads.Queries.GetTmpVoucherHeadById;

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
