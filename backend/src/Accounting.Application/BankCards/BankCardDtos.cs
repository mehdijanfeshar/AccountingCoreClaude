using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.BankCards;

/// <summary>حساب بانکی واحد برای کارت حساب جاری.</summary>
public sealed record BankCardAccount(
    Guid Id,
    string AccountNumber,
    string? AccountHolder,
    Guid? BankId,
    Guid? BranchId,
    Guid? AccountCodeId,
    IReadOnlyList<Guid> TafsiliIds);

/// <summary>یک ردیف کارت حساب جاری (<c>TB_BANKCARTDETAIL</c>). واریز = بستانکار کارت، برداشت = بدهکار کارت.</summary>
public sealed record BankCardRowDto(
    Guid Id,
    string? Date,
    string? Month,
    string? Number,
    CheckReceiptType? Type,
    decimal Deposit,
    decimal Withdrawal,
    bool IsReconciled,
    Guid? CheckId,
    Guid? ReceiptId);

public sealed record BankCardDto(
    Guid BankAccountId,
    string AccountNumber,
    string Year,
    string Month,
    IReadOnlyList<BankCardRowDto> Rows,
    decimal TotalDeposit,
    decimal TotalWithdrawal,
    int ReconciledCount);

/// <summary>ردیف سند روی حساب بانکی که چک/فیش دارد ولی هنوز در کارت مغایرت‌گیری نشده.</summary>
public sealed record BankCardBookItemDto(
    Guid VoucherDetailId,
    Guid VoucherHeadId,
    string? VoucherNumber,
    string? VoucherDate,
    string? Number,
    string? Description,
    decimal Debit,
    decimal Credit);

/// <summary>
/// صورت مغایرت کارت حساب جاری تا پایان ماه انتخابی. مانده طبق بانک در دیسکت نیست و در صفحه وارد می‌شود.
/// </summary>
public sealed record BankCardReconciliationDto(
    Guid BankAccountId,
    string AccountNumber,
    string? AccountHolder,
    string Year,
    string Month,
    string ToDate,
    decimal BookBalance,
    IReadOnlyList<BankCardRowDto> BankOnlyDeposits,
    IReadOnlyList<BankCardRowDto> BankOnlyWithdrawals,
    IReadOnlyList<BankCardBookItemDto> BookOnlyDeposits,
    IReadOnlyList<BankCardBookItemDto> BookOnlyPayments);

/// <summary>چک/فیش دفتر که با ردیف کارت جفت شد.</summary>
public sealed record BankCardMatch(Guid DocumentId, string Number, decimal Amount);

public static class BankCardRowMapper
{
    public static BankCardRowDto ToDto(TB_BANKCARTDETAIL r) => new(
        r.ID,
        r.RECIVDATE,
        r.MONTH,
        r.CHEQNO,
        r.CHECKRECEIPTTYPE,
        r.CREDITOR ?? 0,
        r.DEBTOR ?? 0,
        r.CHECK_ID != null || r.RECEIP_ID != null,
        r.CHECK_ID,
        r.RECEIP_ID);
}
