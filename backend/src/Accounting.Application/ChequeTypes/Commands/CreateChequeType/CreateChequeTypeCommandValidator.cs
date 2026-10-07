using FluentValidation;

namespace Accounting.Application.ChequeTypes.Commands.CreateChequeType;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Layout fields are <c>short?</c> with explicit 0..999 / 0..9999 range rules matching the
/// Oracle precision (3 or 4 digits) — they were <c>byte</c> until risk #24 was closed.
///
/// <see cref="CreateChequeTypeCommand.ChequeImage"/> carries no size-limit rule — see the
/// command's XML doc for why that is a deliberately unmade decision, not an oversight.
///
/// The <c>RuleFor(x => x.VahedCode)</c> below is a deliberate second belt, not dead code: by the
/// time this validator runs, <c>VahedScopeBehavior</c> (registered ahead of
/// <c>ValidationBehavior</c> — see <c>DependencyInjection.cs</c>) has already overwritten
/// <see cref="CreateChequeTypeCommand.VahedCode"/> with the server-assigned value, so this rule
/// now validates that value rather than anything the caller supplied.
/// </summary>
public sealed class CreateChequeTypeCommandValidator : AbstractValidator<CreateChequeTypeCommand>
{
    public CreateChequeTypeCommandValidator()
    {
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
