using FluentValidation;

namespace Accounting.Application.TmpVoucherHeads.Commands.UpdateTmpVoucherHead;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c> and mirroring <c>CreateTmpVoucherHeadCommandValidator</c> rule for
/// rule, plus the route-bound <c>Id</c>. Per the recorded "Legacy fully replaces the rich model"
/// architecture decision, accounting invariants must NOT be re-created here.
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
public sealed class UpdateTmpVoucherHeadCommandValidator : AbstractValidator<UpdateTmpVoucherHeadCommand>
{
    public UpdateTmpVoucherHeadCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

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
