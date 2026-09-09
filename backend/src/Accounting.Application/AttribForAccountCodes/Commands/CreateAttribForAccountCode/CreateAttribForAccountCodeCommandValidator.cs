using FluentValidation;

namespace Accounting.Application.AttribForAccountCodes.Commands.CreateAttribForAccountCode;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Does NOT pre-check <c>AK_AK_ATTRIBFORMAINCO_ATTRIBFO</c> uniqueness —
/// that is left to the DB constraint, mapped centrally to 409 by <c>UnitOfWork</c>. Does NOT
/// pre-check that <see cref="CreateAttribForAccountCodeCommand.AccountCodeId"/> references an
/// existing row — that is left to <c>FK_ATTRIBFO_ACCOUNTCODE</c>, mapped centrally to 400.
/// </summary>
public sealed class CreateAttribForAccountCodeCommandValidator : AbstractValidator<CreateAttribForAccountCodeCommand>
{
    public CreateAttribForAccountCodeCommandValidator()
    {
        RuleFor(x => x.AccountCodeId)
            .NotEmpty();

        // Deliberate second belt, not dead code: by the time this validator runs,
        // VahedScopeBehavior (registered ahead of ValidationBehavior — see
        // DependencyInjection.cs) has already overwritten CreateAttribForAccountCodeCommand.VahedCode
        // with the server-assigned value, so this rule now validates that value rather than
        // anything the caller supplied.
        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);

        RuleFor(x => x.Year)
            .NotEmpty()
            .MaximumLength(4);
    }
}
