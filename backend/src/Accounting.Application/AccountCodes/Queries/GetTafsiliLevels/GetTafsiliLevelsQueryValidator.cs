using FluentValidation;

namespace Accounting.Application.AccountCodes.Queries.GetTafsiliLevels;

/// <summary>
/// Surface-level (syntactic) validation only — rejects an empty <see cref="Guid"/>.
/// </summary>
public sealed class GetTafsiliLevelsQueryValidator : AbstractValidator<GetTafsiliLevelsQuery>
{
    public GetTafsiliLevelsQueryValidator()
    {
        RuleFor(x => x.AccountCodeId)
            .NotEqual(Guid.Empty);
    }
}
