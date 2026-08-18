using FluentValidation;

namespace Accounting.Application.Vouchers.Queries.GetVoucherHeadById;

/// <summary>
/// Surface-level (syntactic) validation only — rejects an empty <see cref="Guid"/>.
/// </summary>
public sealed class GetVoucherHeadByIdQueryValidator : AbstractValidator<GetVoucherHeadByIdQuery>
{
    public GetVoucherHeadByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);
    }
}
