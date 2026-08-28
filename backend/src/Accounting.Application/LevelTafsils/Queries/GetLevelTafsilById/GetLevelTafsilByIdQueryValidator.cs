using FluentValidation;

namespace Accounting.Application.LevelTafsils.Queries.GetLevelTafsilById;

/// <summary>
/// Surface-level (syntactic) validation only — rejects an empty <see cref="Guid"/>.
/// </summary>
public sealed class GetLevelTafsilByIdQueryValidator : AbstractValidator<GetLevelTafsilByIdQuery>
{
    public GetLevelTafsilByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);
    }
}
