using FluentValidation;

namespace Accounting.Application.TmpVoucherHeads.Commands.CreateTmpVoucherHead;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model" architecture
/// decision, accounting invariants must NOT be re-created here. Every column on
/// <c>TB_TMP_VOUCHERHEAD</c> is nullable, so there are no <c>NotEmpty</c>/
/// <c>NotEqual(Guid.Empty)</c> rules anywhere in this validator — only <c>MaximumLength</c> on
/// the string columns. <c>SysType</c>'s single-character limit comes straight from
/// <c>HasMaxLength(1)</c>; no allowed-value rule is invented for it, because the permitted set is
/// unknown (see <see cref="CreateTmpVoucherHeadCommand"/> XML doc).
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
            .MaximumLength(4);

        RuleFor(x => x.Year)
            .MaximumLength(4);

        RuleFor(x => x.SysType)
            .MaximumLength(1);
    }
}
