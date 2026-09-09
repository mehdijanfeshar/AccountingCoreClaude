using FluentValidation;

namespace Accounting.Application.PreDescribs.Commands.CreatePreDescrib;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model"
/// architecture decision, accounting invariants were deliberately discarded and must NOT be
/// re-created here.
///
/// The <c>RuleFor(x => x.VahedCode)</c> below is a deliberate second belt, not dead code: by the
/// time this validator runs, <c>VahedScopeBehavior</c> (registered ahead of
/// <c>ValidationBehavior</c> — see <c>DependencyInjection.cs</c>) has already overwritten
/// <see cref="CreatePreDescribCommand.VahedCode"/> with the server-assigned value, so this rule
/// now validates that value rather than anything the caller supplied. <c>NotEmpty</c> is applied
/// even though the underlying <c>VAHEDCODE</c> column is nullable at the Legacy schema level —
/// the caller can never leave this field blank any more, since it is never client-supplied.
/// </summary>
public sealed class CreatePreDescribCommandValidator : AbstractValidator<CreatePreDescribCommand>
{
    public CreatePreDescribCommandValidator()
    {
        RuleFor(x => x.Descrip)
            .MaximumLength(200);

        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);
    }
}
