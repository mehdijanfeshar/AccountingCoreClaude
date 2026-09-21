using FluentValidation;

namespace Accounting.Application.Vouchers.Commands.UpdateVoucherHead;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model"
/// architecture decision, accounting invariants (debit==credit balance, post-immutability,
/// required-detail, etc.) were deliberately discarded and must NOT be re-created here.
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
public sealed class UpdateVoucherHeadCommandValidator : AbstractValidator<UpdateVoucherHeadCommand>
{
    public UpdateVoucherHeadCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x)
            .Must(x => x.ParentHeadId != x.Id)
            .WithMessage("ParentHeadId cannot be the row's own Id — this would create a self-referencing cycle in the voucher head hierarchy.")
            .WithName("ParentHeadId");

        RuleFor(x => x.DocNum)
            .NotEmpty()
            .MaximumLength(6);

        RuleFor(x => x.DateDoc)
            .NotEmpty()
            .MaximumLength(8);

        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);

        RuleFor(x => x.Year)
            .NotEmpty()
            .MaximumLength(4);

        RuleFor(x => x.HeadDesc)
            .MaximumLength(250);

        RuleFor(x => x.Apendix)
            .MaximumLength(800);

        RuleFor(x => x.SndVahedCode)
            .MaximumLength(4);

        RuleFor(x => x.AttachFileName)
            .MaximumLength(100);

        RuleFor(x => x.AtfNum)
            .MaximumLength(15);

        // .IsInEnum() only rejects an out-of-range underlying integer (e.g. (DocLife)99). Null
        // still passes — the column is optional. ⚠️ Note this rule also rejects 0, which is the
        // Oracle column's own DEFAULT; see the DocLife XML doc for why 0 was not added to the
        // enum and what that means for editing any pre-existing row that carries it.
        //
        // ⚠️ This rule does NOT address the separate open risk that this command lets DOCLIFE be
        // changed at all: the reference project keeps state transitions in their own
        // ChangeState command and its update path never touches the column.
        RuleFor(x => x.DocLife)
            .IsInEnum();
    }
}
