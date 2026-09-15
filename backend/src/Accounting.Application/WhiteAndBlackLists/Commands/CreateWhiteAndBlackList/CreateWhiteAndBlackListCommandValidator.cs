using FluentValidation;

namespace Accounting.Application.WhiteAndBlackLists.Commands.CreateWhiteAndBlackList;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model"
/// architecture decision, accounting invariants were deliberately discarded and must NOT be
/// re-created here.
/// </summary>
public sealed class CreateWhiteAndBlackListCommandValidator : AbstractValidator<CreateWhiteAndBlackListCommand>
{
    public CreateWhiteAndBlackListCommandValidator()
    {
        RuleFor(x => x.AccountCodeId)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.FromAuthorizedDate)
            .MaximumLength(8);

        RuleFor(x => x.ToAuthorizedDate)
            .MaximumLength(8);

        RuleFor(x => x.FromLimitationDate)
            .MaximumLength(8);

        RuleFor(x => x.ToLimitationDate)
            .MaximumLength(8);
    }
}
