using FluentValidation;

namespace Accounting.Application.TmpVoucherHeads.Commands.CreateTmpVoucherHead;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model" architecture
/// decision, accounting invariants must NOT be re-created here. Every column on
/// <c>TB_TMP_VOUCHERHEAD</c> that reaches this command as a positional parameter is nullable, so
/// there are no <c>NotEmpty</c>/<c>NotEqual(Guid.Empty)</c> rules on those — only
/// <c>MaximumLength</c> on the string columns. <c>SysType</c>'s single-character limit comes
/// straight from <c>HasMaxLength(1)</c>; no allowed-value rule is invented for it, because the
/// permitted set is unknown (see <see cref="CreateTmpVoucherHeadCommand"/> XML doc).
///
/// The <c>RuleFor(x => x.VahedCode)</c> below is the one exception, and a deliberate second
/// belt, not dead code: by the time this validator runs, <c>VahedScopeBehavior</c> (registered
/// ahead of <c>ValidationBehavior</c> — see <c>DependencyInjection.cs</c>) has already
/// overwritten <see cref="CreateTmpVoucherHeadCommand.VahedCode"/> with the server-assigned
/// value, so this rule now validates that value rather than anything the caller supplied.
/// <c>NotEmpty</c> is applied even though the underlying <c>VAHEDCODE</c> column is nullable at
/// the Legacy schema level — the caller can never leave this field blank any more, since it is
/// never client-supplied.
/// </summary>
public sealed class CreateTmpVoucherHeadCommandValidator : AbstractValidator<CreateTmpVoucherHeadCommand>
{
    public CreateTmpVoucherHeadCommandValidator()
    {
        RuleFor(x => x.DateDoc)
            .MaximumLength(8);

        RuleFor(x => x.HeadDesc)
            .MaximumLength(250);

        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);

        RuleFor(x => x.Year)
            .MaximumLength(4);

        RuleFor(x => x.SysType)
            .MaximumLength(1);
    }
}
