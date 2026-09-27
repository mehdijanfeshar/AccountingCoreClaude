namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// The two buckets of a fund's outstanding exposure used by the §2 cash-balance equation
/// (<c>docs/tankhah-khazaneh-module.md</c>): <c>موجودی نقد = DEFAULTAMOUNT − Σ(مبلغ کل اسناد در
/// وضعیت New, PendingReview, Returned, Approved)</c>, split here into "already approved,
/// awaiting ترمیم" and "still in flight" because both the funds-list DTO and the Submit balance
/// check need them separately (the DTO surfaces them as two distinct numbers;
/// <c>PettyCashInsufficientCashBalanceException</c> only needs their sum).
/// </summary>
/// <param name="ApprovedAmount">Sum of (AmountBeforeTax + VatAmount) over non-deleted documents in <see cref="Accounting.Domain.ValueObjects.PettyCashDocState.Approved"/>.</param>
/// <param name="ApprovedCount">Count of the same set.</param>
/// <param name="InFlightAmount">Sum of (AmountBeforeTax + VatAmount) over non-deleted documents in New, PendingReview or Returned.</param>
/// <param name="InFlightCount">Count of the same set.</param>
public sealed record PettyCashFundExposure(
    decimal ApprovedAmount,
    int ApprovedCount,
    decimal InFlightAmount,
    int InFlightCount);
