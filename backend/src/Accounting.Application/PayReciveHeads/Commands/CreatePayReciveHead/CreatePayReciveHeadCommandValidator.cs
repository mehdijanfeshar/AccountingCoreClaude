using FluentValidation;

namespace Accounting.Application.PayReciveHeads.Commands.CreatePayReciveHead;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model" architecture
/// decision, accounting invariants must NOT be re-created here — in particular the reference
/// project's duplicate-document-number guard is deliberately absent (see
/// <see cref="CreatePayReciveHeadCommand"/> XML doc: the table has no UNIQUE constraint to back
/// it). <c>PayReciveType</c> carries no range rule either; inventing one would be fabricating a
/// business rule on a column whose CLR type is already known to be wrong.
///
/// The five <c>NotEmpty</c> rules below are not invented: they mirror NOT NULL columns in the
/// Oracle schema (<c>PAYRECIVCODE</c>, <c>PAYRECIVDATE</c>, <c>PAYRECIVDESCRIPTION</c>,
/// <c>VAHEDCODE</c>, <c>YEAR</c>), which is exactly the same reasoning used by
/// <c>CreateReceiptCommandValidator</c>.
///
/// The <c>RuleFor(x => x.VahedCode)</c> below is a deliberate second belt, not dead code: by the
/// time this validator runs, <c>VahedScopeBehavior</c> (registered ahead of
/// <c>ValidationBehavior</c> — see <c>DependencyInjection.cs</c>) has already overwritten
/// <see cref="CreatePayReciveHeadCommand.VahedCode"/> with the server-assigned value, so this
/// rule now validates that value rather than anything the caller supplied.
/// </summary>
public sealed class CreatePayReciveHeadCommandValidator : AbstractValidator<CreatePayReciveHeadCommand>
{
    public CreatePayReciveHeadCommandValidator()
    {
        RuleFor(x => x.PayReciveCode)
            .NotEmpty()
            .MaximumLength(5);

        RuleFor(x => x.PayReciveDate)
            .NotEmpty()
            .MaximumLength(8);

        RuleFor(x => x.PayReciveDescription)
            .NotEmpty()
            .MaximumLength(250);

        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);

        RuleFor(x => x.Year)
            .NotEmpty()
            .MaximumLength(4);
    }
}
