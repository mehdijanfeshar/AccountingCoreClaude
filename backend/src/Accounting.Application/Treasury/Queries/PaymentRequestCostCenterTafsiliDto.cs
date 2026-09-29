namespace Accounting.Application.Treasury.Queries;

/// <summary>
/// One <c>TB_TR_PAYMENT_REQUEST_LINK_TAFSILI</c> row, as returned by <c>GET
/// api/treasury/payment-requests/{id}</c> — اصلاح ۴-الف (۲۰۲۶-۰۹-۲۹). Replaces the single
/// <c>CostCenterTafsiliId</c> field that <see cref="PaymentRequestDto"/> used to carry.
/// </summary>
/// <param name="LevelId">LEVEL_ID column — TB_LEVEL_TAFSIL.ID.</param>
/// <param name="LevelName">Display — TB_LEVEL_TAFSIL.LEVEL_NAME.</param>
/// <param name="TafsiliId">TAFSILI_ID column — TB_TAFSILI.ID.</param>
/// <param name="TafsiliCode">Display — TB_TAFSILI.TAFSILI_CODE.</param>
/// <param name="TafsiliName">Display — TB_TAFSILI.TAFSILI_NAME.</param>
public sealed record PaymentRequestCostCenterTafsiliDto(
    Guid LevelId,
    string? LevelName,
    Guid TafsiliId,
    string? TafsiliCode,
    string? TafsiliName);
