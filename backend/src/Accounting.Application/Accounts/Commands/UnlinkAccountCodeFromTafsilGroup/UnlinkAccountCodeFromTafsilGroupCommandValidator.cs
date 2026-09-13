using FluentValidation;

namespace Accounting.Application.Accounts.Commands.UnlinkAccountCodeFromTafsilGroup;

/// <summary>
/// Surface-level validation only — the two fields are both route-bound.
/// </summary>
public sealed class UnlinkAccountCodeFromTafsilGroupCommandValidator : AbstractValidator<UnlinkAccountCodeFromTafsilGroupCommand>
{
    public UnlinkAccountCodeFromTafsilGroupCommandValidator()
    {
        RuleFor(x => x.AccountCodeId)
            .NotEmpty();

        RuleFor(x => x.LinkId)
            .NotEmpty();
    }
}
