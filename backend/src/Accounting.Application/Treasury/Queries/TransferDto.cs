using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Treasury.Queries;

/// <summary><c>GET api/treasury/transfers/{id}</c> — full detail + «گردش عملیات».</summary>
public sealed record TransferDto(
    Guid Id,
    string Code,
    Guid SourceBankAccountId,
    Guid DestBankAccountId,
    decimal Amount,
    string TransferDate,
    TreasuryPaymentMethod TransferMethod,
    string Reason,
    TransferState State,
    string? BankReference,
    Guid? VoucherId,
    string? VoucherNumber,
    string? ApprovedBy,
    DateTime? ApprovedDate,
    string? ReturnReason,
    string AddUserId,
    DateTime CreatedDate,
    IReadOnlyList<TransferEventDto> Events);
