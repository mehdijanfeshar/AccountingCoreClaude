using FluentValidation;

namespace Accounting.Application.Vouchers.Commands.UpdateVoucherHead;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model"
/// architecture decision, accounting invariants (debit==credit balance, post-immutability,
/// required-detail, etc.) were deliberately discarded and must NOT be re-created here.
///
/// ⚠️ The <c>RuleFor(x => x.VahedCode)</c> below currently does NOT execute at runtime:
/// <c>ValidationBehavior</c>'s <c>where TRequest : IRequest&lt;TResponse&gt;</c> constraint is
/// never satisfied for this void (<c>: IRequest</c>) command in MediatR 14.2.0, so the DI
/// container silently skips this validator for every <c>Update</c>/<c>Delete</c> request
/// project-wide — see <c>docs/open-decisions.md</c> and <c>VahedScopeBehavior.cs</c> (which
/// deliberately avoids the same constraint for this exact reason).
/// <see cref="UpdateVoucherHeadCommand.VahedCode"/> is still safe at runtime only because
/// <c>VahedScopeBehavior</c> unconditionally overwrites it before the handler runs, not because
/// of this rule.
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
    }
}
