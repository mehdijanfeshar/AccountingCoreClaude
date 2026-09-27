namespace Accounting.Application.PettyCash.Queries;

/// <summary>
/// Read-side projection of one <c>TB_REVOLVING_FUND</c> row plus its (optional)
/// <c>TB_PC_FUND_SETTING</c> and its computed §2 balance summary — the
/// <c>GET api/petty-cash/funds</c> list item, per <c>docs/tankhah-khazaneh-module.md</c> §5.
/// </summary>
/// <param name="Id">TB_REVOLVING_FUND.ID.</param>
/// <param name="Code">TB_REVOLVING_FUND.CODE.</param>
/// <param name="Name">TB_REVOLVING_FUND.NAME.</param>
/// <param name="Ceiling">TB_REVOLVING_FUND.DEFAULTAMOUNT — سقف.</param>
/// <param name="AccountCodeId">TB_REVOLVING_FUND.ACCOUNTCODE_ID.</param>
/// <param name="AccountCodeTitle">Display-only: <c>TB_ACCOUNTCODE.ACCCODENAME</c> of the linked معین.</param>
/// <param name="Settings"><see langword="null"/> when the fund has no <c>TB_PC_FUND_SETTING</c> row yet.</param>
/// <param name="CashBalance">§2: <c>Ceiling − (ApprovedAmount + InFlightAmount)</c>.</param>
/// <param name="ApprovedAmount">Sum of documents currently تأییدشده (منتظر ترمیم).</param>
/// <param name="ApprovedCount">Count of the same set.</param>
/// <param name="InFlightAmount">Sum of documents currently جدید/در انتظار بررسی/برگشتی.</param>
/// <param name="InFlightCount">Count of the same set.</param>
public sealed record PettyCashFundDto(
    Guid Id,
    string Code,
    string Name,
    decimal? Ceiling,
    Guid? AccountCodeId,
    string? AccountCodeTitle,
    PettyCashFundSettingDto? Settings,
    decimal CashBalance,
    decimal ApprovedAmount,
    int ApprovedCount,
    decimal InFlightAmount,
    int InFlightCount);
