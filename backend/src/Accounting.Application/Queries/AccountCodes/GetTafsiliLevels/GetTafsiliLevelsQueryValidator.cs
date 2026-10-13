using FluentValidation;

namespace Accounting.Application.Queries.AccountCodes.GetTafsiliLevels;

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
