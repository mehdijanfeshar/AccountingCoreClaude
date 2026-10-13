using FluentValidation;

namespace Accounting.Application.Queries.VahedInfos.GetVahedInfoById;

/// <summary>
/// Surface-level (syntactic) validation only — rejects an empty <see cref="Guid"/>.
/// </summary>
public sealed class GetVahedInfoByIdQueryValidator : AbstractValidator<GetVahedInfoByIdQuery>
{
    public GetVahedInfoByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);
    }
}
