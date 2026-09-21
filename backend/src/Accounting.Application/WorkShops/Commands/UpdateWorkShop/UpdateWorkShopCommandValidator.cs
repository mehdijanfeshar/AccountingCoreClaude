using FluentValidation;

namespace Accounting.Application.WorkShops.Commands.UpdateWorkShop;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model" architecture
/// decision, accounting invariants must NOT be re-created here.
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
public sealed class UpdateWorkShopCommandValidator : AbstractValidator<UpdateWorkShopCommand>
{
    public UpdateWorkShopCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.AccountCodeId)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.WorkShopName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.WorkShopCode)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);
    }
}
