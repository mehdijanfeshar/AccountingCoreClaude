using FluentValidation;

namespace Accounting.Application.Accounts.Commands.UpdateAccountTafsilGroupLink;

/// <summary>
/// Surface-level validation only.
/// </summary>
public sealed class UpdateAccountTafsilGroupLinkCommandValidator : AbstractValidator<UpdateAccountTafsilGroupLinkCommand>
{
    public UpdateAccountTafsilGroupLinkCommandValidator()
    {
        RuleFor(x => x.AccountCodeId)
            .NotEmpty();

        RuleFor(x => x.LinkId)
            .NotEmpty();

        RuleFor(x => x.LevelId)
            .NotEmpty();

        RuleFor(x => x.TafsilGroupId)
            .NotEmpty();
    }
}
