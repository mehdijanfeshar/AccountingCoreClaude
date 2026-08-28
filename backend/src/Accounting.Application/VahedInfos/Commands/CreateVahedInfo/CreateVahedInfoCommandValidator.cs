using FluentValidation;

namespace Accounting.Application.VahedInfos.Commands.CreateVahedInfo;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. <see cref="CreateVahedInfoCommand.CityId"/> and
/// <see cref="CreateVahedInfoCommand.ParentId"/> carry no format rule beyond
/// presence/nullability — inventing a referential-integrity check here (e.g. that
/// <c>CityId</c> exists) would be a business rule this project has not been asked to add; both
/// columns have no mapped FK constraint in the DB either. Per the recorded "Legacy fully
/// replaces the rich model" architecture decision, accounting invariants must NOT be
/// re-created here.
/// </summary>
public sealed class CreateVahedInfoCommandValidator : AbstractValidator<CreateVahedInfoCommand>
{
    public CreateVahedInfoCommandValidator()
    {
        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);

        RuleFor(x => x.VahedName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.CityId)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.VahedTypeId)
            .NotEqual(Guid.Empty);
    }
}
