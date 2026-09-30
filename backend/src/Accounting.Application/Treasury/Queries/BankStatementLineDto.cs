using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Treasury.Queries;

/// <summary>One line of <c>GET api/treasury/statements/{id}</c>.</summary>
/// <param name="MatchedVoucherNumber">نمایشی — <c>TB_VOUCHERSHEAD.DOC_NUM</c> سند صاحب
/// <see cref="MatchedVoucherDetailId"/>، فقط وقتی تطبیق‌یافته باشد.</param>
/// <param name="ResolutionVoucherNumber">نمایشی، فقط وقتی <see cref="ResolutionType"/> =
/// <see cref="BankStatementLineResolutionType.BankFeeVoucher"/>.</param>
/// <param name="ResolutionReceiptCode">نمایشی، فقط وقتی <see cref="ResolutionType"/> =
/// <see cref="BankStatementLineResolutionType.LinkedReceipt"/>.</param>
public sealed record BankStatementLineDto(
    Guid Id,
    Guid StatementId,
    string LineDate,
    string? BankReference,
    string? Description,
    decimal Withdrawal,
    decimal Deposit,
    decimal? Balance,
    BankStatementLineMatchState MatchState,
    Guid? MatchedVoucherDetailId,
    string? MatchedVoucherNumber,
    BankStatementLineResolutionType? ResolutionType,
    Guid? ResolutionVoucherId,
    string? ResolutionVoucherNumber,
    Guid? ResolutionReceiptId,
    string? ResolutionReceiptCode,
    string? ResolutionNote,
    string AddUserId,
    DateTime CreatedDate);
