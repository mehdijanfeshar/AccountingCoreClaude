using FluentValidation;

namespace Accounting.Application.Accounts.Commands.LinkAccountCodeToTafsilGroup;

/// <summary>
/// Surface-level (syntactic) validation only. Does NOT pre-check <c>UK_ACCOUNTLINKTAFSILGROUP</c>
/// uniqueness or FK existence — both are left to the DB constraints, mapped centrally to 409/400
/// by <c>UnitOfWork</c>.
/// </summary>
public sealed class LinkAccountCodeToTafsilGroupCommandValidator : AbstractValidator<LinkAccountCodeToTafsilGroupCommand>
{
    public LinkAccountCodeToTafsilGroupCommandValidator()
    {
        RuleFor(x => x.AccountCodeId)
            .NotEmpty();

        RuleFor(x => x.LevelId)
            .NotEmpty();

        RuleFor(x => x.TafsilGroupId)
            .NotEmpty();
    }
}
