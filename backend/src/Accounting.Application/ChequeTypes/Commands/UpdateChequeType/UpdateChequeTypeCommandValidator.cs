using FluentValidation;

namespace Accounting.Application.ChequeTypes.Commands.UpdateChequeType;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Same rationale as <c>CreateChequeTypeCommandValidator</c> for why no
/// range rule is applied to the <c>byte?</c> layout fields and no size-limit rule is applied to
/// <c>ChequeImage</c>.
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
public sealed class UpdateChequeTypeCommandValidator : AbstractValidator<UpdateChequeTypeCommand>
{
    public UpdateChequeTypeCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        // NUMBER(3) ⇒ 0..999، NUMBER(4) ⇒ 0..9999 (پس از عریض‌شدن byte⇒short، ریسک #۲۴).
        RuleFor(x => x.ChequeWidth).InclusiveBetween((short)0, (short)999);
        RuleFor(x => x.ChequeHeight).InclusiveBetween((short)0, (short)999);
        RuleFor(x => x.ChequeAdateLeft).InclusiveBetween((short)0, (short)9999);
        RuleFor(x => x.ChequeAdateTop).InclusiveBetween((short)0, (short)9999);
        RuleFor(x => x.ChequeAdateWidth).InclusiveBetween((short)0, (short)9999);
        RuleFor(x => x.ChequeNdateLeft).InclusiveBetween((short)0, (short)9999);
        RuleFor(x => x.ChequeNdateTop).InclusiveBetween((short)0, (short)9999);
        RuleFor(x => x.ChequeNdateWidth).InclusiveBetween((short)0, (short)9999);
        RuleFor(x => x.ChequeAamountLeft).InclusiveBetween((short)0, (short)9999);
        RuleFor(x => x.ChequeAamountTop).InclusiveBetween((short)0, (short)9999);
        RuleFor(x => x.ChequeAamountWidth).InclusiveBetween((short)0, (short)9999);
        RuleFor(x => x.ChequeLamountLeft).InclusiveBetween((short)0, (short)9999);
        RuleFor(x => x.ChequeLamountTop).InclusiveBetween((short)0, (short)9999);
        RuleFor(x => x.ChequeLamountWidth).InclusiveBetween((short)0, (short)9999);
        RuleFor(x => x.ChequeNamountLeft).InclusiveBetween((short)0, (short)9999);
        RuleFor(x => x.ChequeNamountTop).InclusiveBetween((short)0, (short)9999);
        RuleFor(x => x.ChequeNamountWidth).InclusiveBetween((short)0, (short)9999);
        RuleFor(x => x.ChequeDescribe1Left).InclusiveBetween((short)0, (short)9999);
        RuleFor(x => x.ChequeDescribe1Top).InclusiveBetween((short)0, (short)9999);
        RuleFor(x => x.ChequeDescribe1Width).InclusiveBetween((short)0, (short)9999);
        RuleFor(x => x.ChequeDescribe2Left).InclusiveBetween((short)0, (short)9999);
        RuleFor(x => x.ChequeDescribe2Top).InclusiveBetween((short)0, (short)9999);
        RuleFor(x => x.ChequeDescribe2Width).InclusiveBetween((short)0, (short)9999);
        RuleFor(x => x.ChequeBreaklineLeft).InclusiveBetween((short)0, (short)9999);
        RuleFor(x => x.ChequeBreaklineTop).InclusiveBetween((short)0, (short)9999);
        RuleFor(x => x.ChequeBreaklineWidth).InclusiveBetween((short)0, (short)9999);

        RuleFor(x => x.ChequeTypeTitle)
            .MaximumLength(25);

        RuleFor(x => x.ChequeAdateFont)
            .MaximumLength(200);

        RuleFor(x => x.ChequeNdateFont)
            .MaximumLength(200);

        RuleFor(x => x.ChequeAamountFont)
            .MaximumLength(200);

        RuleFor(x => x.ChequeLamountFont)
            .MaximumLength(200);

        RuleFor(x => x.ChequeNamountFont)
            .MaximumLength(200);

        RuleFor(x => x.ChequeDescribe1Font)
            .MaximumLength(200);

        RuleFor(x => x.ChequeDescribe2Font)
            .MaximumLength(200);

        RuleFor(x => x.ChequeBreaklineFont)
            .MaximumLength(200);

        // حاشیهٔ چاپگر می‌تواند منفی باشد (عین سیستم قدیم)؛ سقف NUMBER(3).
        RuleFor(x => x.PrinterMargineTop)
            .InclusiveBetween((short)-999, (short)999).WithMessage("حاشیهٔ بالا باید بین ۹۹۹- و ۹۹۹ میلی‌متر باشد.");
        RuleFor(x => x.PrinterMargineLeft)
            .InclusiveBetween((short)-999, (short)999).WithMessage("حاشیهٔ چپ باید بین ۹۹۹- و ۹۹۹ میلی‌متر باشد.");
        RuleFor(x => x.PrinterType)
            .MaximumLength(100);

        RuleFor(x => x.Year)
            .NotEmpty()
            .MaximumLength(4);

        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);
    }
}
