namespace Accounting.Application.SysTypes.Queries;

/// <summary>
/// Read-side projection of <c>TB_SYSTYPE</c> — the نوع سند lookup referenced by
/// <c>TB_VOUCHERSHEAD.SYSTEM_TYPE</c> (حسابداري، اموال، حقوق و دستمزد، دريافت و پرداخت،
/// افتتاحيه، اعلاميهٔ صادره/رسيده). A small, static reference table: it carries no
/// <c>VAHEDCODE</c> and no logical-delete flag, so it is neither unit-scoped nor filtered.
/// </summary>
/// <param name="Id">ID column.</param>
/// <param name="SysCode">SYS_COD column — the Legacy numeric code as a string.</param>
/// <param name="SysName">SYS_NAME column — display name.</param>
public sealed record SysTypeDto(Guid Id, string SysCode, string? SysName);
