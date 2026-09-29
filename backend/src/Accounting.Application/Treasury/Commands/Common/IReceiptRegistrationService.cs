using Accounting.Domain.Entity;

namespace Accounting.Application.Treasury.Commands.Common;

/// <summary>
/// Shared orchestration for turning a پیش‌نویس دریافت وجه into a
/// <see cref="Accounting.Domain.ValueObjects.ReceiptState.Registered"/> one — خزانه‌داری، بخش ۴-ج.
/// Used by both the standalone <c>RegisterReceiptCommand</c> and
/// <c>CreateReceiptCommand</c>'s <c>register=true</c> shortcut, so the two call sites can never
/// drift apart (same reason بخش-۴-الف shares <c>IPaymentRequestSubmitRuleChecker</c> between
/// Create+submit and standalone Submit). Stages every write (voucher + Legacy
/// <c>TB_PAYRECIVHEAD/DETAIL/LINK_TAFSILI</c> + the receipt row's own fields) but never calls
/// <see cref="Accounting.Application.Common.Interfaces.IUnitOfWork.SaveChangesAsync"/> — the
/// caller owns the explicit transaction boundary (سند + Legacy write + state transition, same
/// posture as <c>ExecutePaymentRequestCommandHandler</c>).
/// </summary>
public interface IReceiptRegistrationService
{
    /// <exception cref="Accounting.Application.Common.Exceptions.TreasuryReceiptStateConflictException">
    /// <paramref name="receipt"/> is not <see cref="Accounting.Domain.ValueObjects.ReceiptState.Draft"/>.
    /// </exception>
    /// <exception cref="Accounting.Application.Common.Exceptions.TreasuryTreasurerRoleRequiredException">
    /// The caller holds no active Treasurer role in <paramref name="vahedCode"/>.
    /// </exception>
    Task RegisterAsync(TB_TR_RECEIPT receipt, string vahedCode, CancellationToken cancellationToken = default);
}
