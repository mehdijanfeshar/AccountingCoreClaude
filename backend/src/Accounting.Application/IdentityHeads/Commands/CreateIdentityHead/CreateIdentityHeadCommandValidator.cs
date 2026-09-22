using Accounting.Application.IdentityHeads.Commands.Common;
using FluentValidation;

namespace Accounting.Application.IdentityHeads.Commands.CreateIdentityHead;

/// <summary>
/// Surface-level (syntactic) validation only — lengths and required ids, mirroring the Fluent
/// mapping. Business rules that Legacy itself does not enforce are deliberately absent; see
/// <see cref="CreateIdentityHeadCommand"/>.
/// </summary>
public sealed class CreateIdentityHeadCommandValidator : AbstractValidator<CreateIdentityHeadCommand>
{
    public CreateIdentityHeadCommandValidator()
    {
        RuleFor(x => x.IdentityGroupId)
            .NotEmpty();

        RuleFor(x => x.Year)
            .NotEmpty()
            .MaximumLength(4);

        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);

        // No-op when FixItems is null (a head with no values is allowed) — the explicit `.When`
        // guard, rather than relying on RuleForEach's own null-tolerance, documents that at the
        // call site. Mirrors CreateVoucherHeadCommandValidator's InitialDetails rule.
        RuleForEach(x => x.FixItems)
            .SetValidator(new IdentityHeadFixItemInputValidator())
            .When(x => x.FixItems is not null);

        // One value per subgroup — this is the client-visible half of the real UNIQUE constraint
        // AK_AK_IDENTYFIXITEMS_IDENTYFI. Caught here rather than left to a 409 because a
        // duplicate in a single request is unambiguously a caller mistake, not a race.
        RuleFor(x => x.FixItems)
            .Must(items => items is null
                || items.Select(i => i.IdentitySubGroupId).Distinct().Count() == items.Count)
            .WithMessage("برای هر زیرگروه شناسنامه فقط یک مقدار می‌توان ثبت کرد.");
    }
}

/// <summary>
/// Shared by the create and update validators — the item shape is identical on both paths.
/// </summary>
public sealed class IdentityHeadFixItemInputValidator : AbstractValidator<IdentityHeadFixItemInput>
{
    /// <summary>
    /// <c>TB_IDENTITYFIXITEMS.FIXITEMS_VALUE</c> is <c>VARCHAR2(100)</c> in the Fluent mapping. A
    /// longer value could never be stored, so it is a caller error rather than silent truncation.
    /// </summary>
    public const int MaxValueLength = 100;

    public IdentityHeadFixItemInputValidator()
    {
        RuleFor(x => x.IdentitySubGroupId)
            .NotEmpty();

        RuleFor(x => x.Value)
            .MaximumLength(MaxValueLength);
    }
}
