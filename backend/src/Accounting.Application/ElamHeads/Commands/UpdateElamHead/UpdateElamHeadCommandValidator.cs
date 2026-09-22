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
/// ✅ <b>These rules really do run now — fixed in phase 31; they did not before.</b>
/// <c>ValidationBehavior</c> used to declare <c>where TRequest : IRequest&lt;TResponse&gt;</c>,
/// which MediatR 14 never satisfies for a void (<c>: IRequest</c>) command, so the DI container
/// skipped the validator for every <c>Update</c>/<c>Delete</c> request project-wide — silently,
/// from phase 8 to phase 30. The constraint is gone and
/// <c>BehaviorPipelineConstraintTests</c> fails if it ever comes back. Practical consequence:
/// rules here were written and unit-tested but never exercised against real traffic, so a 400
/// that appears for the first time is most likely this validator finally firing, not a new bug.
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

        // .IsInEnum() only rejects an out-of-range underlying integer (e.g. (ElamCase)99); it says
        // nothing about whether the stored Legacy value is meaningful. Nullable properties pass
        // when null, so "not supplied" stays valid — same shape as the phase 27 batches.
        RuleFor(x => x.Case)
            .IsInEnum();

        RuleFor(x => x.DramadType)
            .IsInEnum();
    }
}
