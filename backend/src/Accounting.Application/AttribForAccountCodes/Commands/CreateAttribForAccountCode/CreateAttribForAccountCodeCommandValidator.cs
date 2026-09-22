using FluentValidation;

namespace Accounting.Application.AttribForAccountCodes.Commands.CreateAttribForAccountCode;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Does NOT pre-check <c>AK_AK_ATTRIBFORMAINCO_ATTRIBFO</c> uniqueness —
/// that is left to the DB constraint, mapped centrally to 409 by <c>UnitOfWork</c>. Does NOT
/// pre-check that <see cref="CreateAttribForAccountCodeCommand.AccountCodeId"/> references an
/// existing row — that is left to <c>FK_ATTRIBFO_ACCOUNTCODE</c>, mapped centrally to 400.
///
/// <c>.IsInEnum()</c> on <see cref="CreateAttribForAccountCodeCommand.Flag"/>,
/// <see cref="CreateAttribForAccountCodeCommand.AttribSum"/> and
/// <see cref="CreateAttribForAccountCodeCommand.ControlId"/> only rejects an out-of-range
/// underlying integer — added in phase 27 batch 2 alongside the <c>bool</c>/<c>bool?</c>-to-enum
/// fix for these three columns. <see cref="CreateAttribForAccountCodeCommand.AttribBoxNo"/> gets
/// <c>InclusiveBetween(0, 9)</c> instead — it is NOT an enum, and the bound comes from the
/// column's physical <c>NUMBER(1)</c> width, not from a known business rule.
/// </summary>
public sealed class CreateAttribForAccountCodeCommandValidator : AbstractValidator<CreateAttribForAccountCodeCommand>
{
    public CreateAttribForAccountCodeCommandValidator()
    {
        RuleFor(x => x.AccountCodeId)
            .NotEmpty();

        RuleFor(x => x.AttribBoxNo)
            .InclusiveBetween((short)0, (short)9);

        RuleFor(x => x.Flag)
            .IsInEnum();

        RuleFor(x => x.AttribSum)
            .IsInEnum();

        RuleFor(x => x.ControlId)
            .IsInEnum()
            .When(x => x.ControlId.HasValue);

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
