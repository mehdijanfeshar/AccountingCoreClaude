namespace Accounting.Application.Treasury.Commands.Common;

/// <summary>
/// Enforces the two تفصیلی rules added by the 2026-09-29 owner decision (اصلاح ۴-الف;
/// <c>docs/tankhah-khazaneh-module.md</c> §۱۰) on every درخواست پرداخت write path (Create and
/// Update alike — "Rule را در Handler کپی نکن"):
/// <list type="bullet">
/// <item><description><b>Cost-center تفصیلی (multi-level).</b> Each
/// <see cref="PaymentRequestTafsiliLinkInput.TafsiliId"/> must exist, and the full set must satisfy
/// <c>IVoucherTafsiliLevelGuard</c> for the request's <c>ExpenseAccountId</c> — every level the
/// حساب هزینه requires must be present, and no unconfigured level may be sent.</description></item>
/// <item><description><b>Beneficiary تفصیلی (optional).</b> When provided, the unit must have a
/// <c>TB_TR_SETTING.BENEFICIARY_TAFSIL_GROUP_ID</c> configured (409 otherwise), and the value must
/// be an active member of that group via <c>TB_TAFSIL_LINK_TAFSILGROUP</c> (400
/// otherwise).</description></item>
/// </list>
/// </summary>
public interface IPaymentRequestTafsiliValidator
{
    /// <summary>
    /// Throws <c>NotFoundException</c> (404) if any <see cref="PaymentRequestTafsiliLinkInput.TafsiliId"/>
    /// does not exist, then delegates to <c>IVoucherTafsiliLevelGuard.EnsureSatisfiedAsync</c> for
    /// the level rule (throws <c>RequiredTafsiliLevelMissingException</c>/
    /// <c>TafsiliLevelNotPermittedException</c>, both 400).
    /// </summary>
    Task EnsureCostCenterTafsilisValidAsync(
        Guid expenseAccountId,
        IReadOnlyList<PaymentRequestTafsiliLinkInput> costCenterTafsilis,
        string vahedCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Does nothing when <paramref name="beneficiaryTafsiliId"/> is <see langword="null"/>. Throws
    /// <c>PaymentRequestBeneficiaryGroupNotConfiguredException</c> (409) when the unit has no
    /// configured group, or <c>PaymentRequestBeneficiaryTafsiliNotInGroupException</c> (400) when
    /// the value is not an active member of that group.
    /// </summary>
    Task EnsureBeneficiaryTafsiliValidAsync(
        Guid? beneficiaryTafsiliId,
        string vahedCode,
        CancellationToken cancellationToken = default);
}
