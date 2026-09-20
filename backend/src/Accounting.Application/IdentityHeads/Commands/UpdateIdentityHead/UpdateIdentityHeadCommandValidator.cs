using Accounting.Application.IdentityHeads.Commands.CreateIdentityHead;
using FluentValidation;

namespace Accounting.Application.IdentityHeads.Commands.UpdateIdentityHead;

/// <summary>
/// Surface-level (syntactic) validation only, mirroring
/// <see cref="CreateIdentityHeadCommandValidator"/> for the parts both paths share.
///
/// ⚠️ <b>Not dead code, despite appearances.</b> <c>ValidationBehavior</c> currently never runs
/// for non-generic <c>IRequest</c> commands (open risk #1-الف), so this validator is not reached
/// today. It is kept — and kept correct — because that bug is a pipeline defect to be fixed, not
/// a reason to stop validating; the moment it is fixed this starts applying.
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
