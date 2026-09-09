using FluentValidation;

namespace Accounting.Application.ElamHeads.Commands.UpdateElamHead;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model" architecture
/// decision, accounting invariants must NOT be re-created here.
///
/// <c>VahedCode</c> gets <c>NotEmpty</c> unlike every other field here, matching
/// <c>CreateElamHeadCommandValidator</c> — see its XML doc for the full rationale.
///
/// ⚠️ Unlike the Create-side validator, the <c>RuleFor(x => x.VahedCode)</c> rule below currently
/// does NOT execute at runtime: <c>ValidationBehavior</c>'s <c>where TRequest : IRequest&lt;TResponse&gt;</c>
/// constraint is never satisfied for this void (<c>: IRequest</c>) command in MediatR 14.2.0, so
/// the DI container silently skips this validator for every <c>Update</c>/<c>Delete</c> request
/// project-wide — see <c>docs/open-decisions.md</c> and <c>VahedScopeBehavior.cs</c> (which
/// deliberately avoids the same constraint for this exact reason). This is safe at runtime only
/// because <c>VahedScopeBehavior</c> unconditionally overwrites <c>VahedCode</c> before the
/// handler runs, not because of this rule.
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
            .NotEmpty()
            .MaximumLength(4);

        RuleFor(x => x.Year)
            .MaximumLength(4);
    }
}
