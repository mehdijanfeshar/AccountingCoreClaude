using Accounting.Application.IdentityHeads.Commands.CreateIdentityHead;
using FluentValidation;

namespace Accounting.Application.IdentityHeads.Commands.UpdateIdentityHead;

/// <summary>
/// Surface-level (syntactic) validation only, mirroring
/// <see cref="CreateIdentityHeadCommandValidator"/> for the parts both paths share.
///
/// ✅ <b>Reached at last (phase 31).</b> This validator used to be unreachable:
/// <c>ValidationBehavior</c> did not run for non-generic <c>IRequest</c> commands (old risk
/// #1-الف), so it was written, kept correct, and never invoked. The pipeline defect is fixed and
/// <c>BehaviorPipelineConstraintTests</c> guards it, so these rules now apply to real traffic.
/// </summary>
public sealed class UpdateIdentityHeadCommandValidator : AbstractValidator<UpdateIdentityHeadCommand>
{
    public UpdateIdentityHeadCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);

        RuleForEach(x => x.FixItems)
            .SetValidator(new IdentityHeadFixItemInputValidator())
            .When(x => x.FixItems is not null);

        RuleFor(x => x.FixItems)
            .Must(items => items is null
                || items.Select(i => i.IdentitySubGroupId).Distinct().Count() == items.Count)
            .WithMessage("برای هر زیرگروه شناسنامه فقط یک مقدار می‌توان ثبت کرد.");
    }
}
