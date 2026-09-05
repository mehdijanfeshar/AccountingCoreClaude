using FluentValidation;

namespace Accounting.Application.ElamHeads.Commands.UpdateElamHead;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model" architecture
/// decision, accounting invariants must NOT be re-created here.
/// </summary>
public sealed class UpdateElamHeadCommandValidator : AbstractValidator<UpdateElamHeadCommand>
{
    public UpdateElamHeadCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

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
