using Accounting.Application.Vouchers.Commands.Common;
using FluentValidation;

namespace Accounting.Application.Vouchers.Commands.UpdateVoucherDetail;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model"
/// architecture decision, accounting invariants were deliberately discarded and must NOT be
/// re-created here.
///
/// ⚠️ The <c>RuleFor(x => x.VahedCode)</c> below currently does NOT execute at runtime:
/// <c>ValidationBehavior</c>'s <c>where TRequest : IRequest&lt;TResponse&gt;</c> constraint is
/// never satisfied for this void (<c>: IRequest</c>) command in MediatR 14.2.0, so the DI
/// container silently skips this validator for every <c>Update</c>/<c>Delete</c> request
/// project-wide — see <c>docs/open-decisions.md</c> and <c>VahedScopeBehavior.cs</c> (which
/// deliberately avoids the same constraint for this exact reason).
/// <see cref="UpdateVoucherDetailCommand.VahedCode"/> is still safe at runtime only because
/// <c>VahedScopeBehavior</c> unconditionally overwrites it before the handler runs, not because
/// of this rule. Unlike before this command implemented
/// <see cref="Accounting.Application.Common.Security.IVahedScopedCommand"/>, <c>VahedCode</c> can
/// no longer legitimately be empty, so <c>NotEmpty</c> was added alongside the pre-existing
/// <c>MaximumLength</c> rule (kept for when the pipeline bug is fixed, and for direct unit
/// testing of this validator).
/// </summary>
public sealed class UpdateVoucherDetailCommandValidator : AbstractValidator<UpdateVoucherDetailCommand>
{
    public UpdateVoucherDetailCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Description)
            .MaximumLength(200);

        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);

        RuleFor(x => x.Year)
            .MaximumLength(4);

        // No-op when TafsiliLinks is null ("leave existing links untouched" — see the command's
        // XML doc for why null and empty differ here). An EMPTY list is still valid and is NOT a
        // validation failure: it is the caller explicitly asking for "no links on this line".
        RuleForEach(x => x.TafsiliLinks)
            .SetValidator(new VoucherDetailTafsiliLinkInputValidator())
            .When(x => x.TafsiliLinks is not null);
    }
}
