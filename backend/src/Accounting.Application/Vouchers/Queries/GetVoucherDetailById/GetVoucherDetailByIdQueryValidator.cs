using FluentValidation;

namespace Accounting.Application.Vouchers.Queries.GetVoucherDetailById;

/// <summary>
/// Surface-level (syntactic) validation only — rejects an empty <see cref="Guid"/>.
/// </summary>
public sealed class GetVoucherDetailByIdQueryValidator : AbstractValidator<GetVoucherDetailByIdQuery>
{
    public GetVoucherDetailByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);
    }
}
