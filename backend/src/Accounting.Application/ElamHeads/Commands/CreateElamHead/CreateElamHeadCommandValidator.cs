using FluentValidation;

namespace Accounting.Application.ElamHeads.Commands.CreateElamHead;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model" architecture
/// decision, accounting invariants must NOT be re-created here. Every field on
/// <see cref="CreateElamHeadCommand"/> is nullable in Legacy, so there are no
/// <c>NotEmpty</c>/<c>NotEqual(Guid.Empty)</c> rules anywhere in this validator — only
/// <c>MaximumLength</c> on the string columns. <c>PrintNo</c>/<c>Case</c>/<c>WebStat</c>/
/// <c>DramadType</c> carry no range rule — see <see cref="CreateElamHeadCommand"/> XML doc for
/// why inventing one would be a fabricated business rule.
/// </summary>
public sealed class CreateElamHeadCommandValidator : AbstractValidator<CreateElamHeadCommand>
{
    public CreateElamHeadCommandValidator()
    {
        RuleFor(x => x.SerialNo)
            .MaximumLength(14);

        RuleFor(x => x.Code)
            .MaximumLength(6);

        RuleFor(x => x.DabirNo)
            .MaximumLength(10);

        RuleFor(x => x.DabirDate)
            .MaximumLength(8);

        RuleFor(x => x.SerialNoInput)
            .MaximumLength(6);

        RuleFor(x => x.Date)
            .MaximumLength(8);

        RuleFor(x => x.Desc)
            .MaximumLength(300);

        RuleFor(x => x.RcvNo)
            .MaximumLength(14);

        RuleFor(x => x.RcvDt)
            .MaximumLength(8);

        RuleFor(x => x.LstMon)
            .MaximumLength(2);

        RuleFor(x => x.PayNo)
            .MaximumLength(15);

        RuleFor(x => x.PeimanNo)
            .MaximumLength(12);

        RuleFor(x => x.WorkShopCode)
            .MaximumLength(10);

        RuleFor(x => x.WorkShopName)
            .MaximumLength(100);

        RuleFor(x => x.SendRcvVahed)
            .MaximumLength(4);

        RuleFor(x => x.ElamYear)
            .MaximumLength(2);

        RuleFor(x => x.VahedCode)
            .MaximumLength(4);

        RuleFor(x => x.Year)
            .MaximumLength(4);
    }
}
