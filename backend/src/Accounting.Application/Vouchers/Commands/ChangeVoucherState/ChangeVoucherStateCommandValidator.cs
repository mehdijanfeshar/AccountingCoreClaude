using FluentValidation;

namespace Accounting.Application.Vouchers.Commands.ChangeVoucherState;

/// <summary>
/// Surface-level validation only.
///
/// ⚠️ <b>Deliberately does NOT validate the transition itself</b> — see
/// <see cref="ChangeVoucherStateCommand"/> for why (no such rule exists to port, and the
/// reference system's own validator checks only <c>IsInEnum</c>).
///
/// ⚠️ Not dead code despite <c>ValidationBehavior</c> not reaching non-generic <c>IRequest</c>
/// commands today (open risk #1-الف) — kept correct so it starts applying the moment that
/// pipeline bug is fixed.
/// </summary>
public sealed class ChangeVoucherStateCommandValidator : AbstractValidator<ChangeVoucherStateCommand>
{
    /// <summary>
    /// Upper bound on one batch. Not a business rule — a guard against a caller sending an
    /// unbounded id list, which would turn into an unbounded IN clause. The کارتابل pages at 20.
    /// </summary>
    public const int MaxBatchSize = 200;

    public ChangeVoucherStateCommandValidator()
    {
        RuleFor(x => x.VoucherHeadIds)
            .NotEmpty()
            .WithMessage("حداقل یک سند باید انتخاب شود.");

        RuleFor(x => x.VoucherHeadIds)
            .Must(ids => ids is null || ids.Count <= MaxBatchSize)
            .WithMessage($"در هر درخواست حداکثر {MaxBatchSize} سند قابل انتقال است.");

        RuleFor(x => x.VoucherHeadIds)
            .Must(ids => ids is null || ids.All(id => id != Guid.Empty))
            .WithMessage("شناسهٔ سند نامعتبر است.");

        // A duplicate id is harmless to apply twice but always means the caller built its
        // selection wrong, so it is reported rather than silently collapsed.
        RuleFor(x => x.VoucherHeadIds)
            .Must(ids => ids is null || ids.Distinct().Count() == ids.Count)
            .WithMessage("شناسهٔ تکراری در فهرست اسناد وجود دارد.");

        RuleFor(x => x.NewState)
            .IsInEnum()
            .WithMessage("وضعیت سند نامعتبر است.");
    }
}
