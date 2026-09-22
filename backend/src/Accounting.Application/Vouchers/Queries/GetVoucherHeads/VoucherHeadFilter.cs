using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Vouchers.Queries.GetVoucherHeads;

/// <summary>
/// The optional filters <c>GetVoucherHeads</c> supports, grouped into one record so the
/// repository signature does not grow a positional parameter per filter. Every member is
/// optional: a <see langword="null"/>/empty value means "do not filter on this column".
///
/// ⚠️ <b>The organizational unit is deliberately NOT a filter here.</b> Unit scope is not
/// caller-controllable at all — <c>GetVoucherHeadsQuery</c> implements <c>IVahedScopedQuery</c>
/// and the repository applies the caller's own <c>VAHEDCODE</c> unconditionally (CLAUDE.md
/// IDOR risk #1). Adding a unit filter here would be the first step back toward that hole.
/// </summary>
/// <param name="Year">Exact-match filter on the YEAR column.</param>
/// <param name="DocNumFrom">Inclusive lower bound of the DOC_NUM range — see the numeric-ordering note on the repository implementation.</param>
/// <param name="DocNumTo">Inclusive upper bound of the DOC_NUM range.</param>
/// <param name="DateDocFrom">Inclusive lower bound of the DATE_DOC range (Legacy <c>YYYYMMDD</c> string).</param>
/// <param name="DateDocTo">Inclusive upper bound of the DATE_DOC range (Legacy <c>YYYYMMDD</c> string).</param>
/// <param name="SystemTypeId">Exact-match filter on SYSTEM_TYPE (نوع سند — FK to <c>TB_SYSTYPE</c>).</param>
/// <param name="DocLife">
/// Exact-match filter on DOCLIFE (وضعیت سند) — this is what backs the کارتابل's per-status tabs.
///
/// ⚠️ <b>Exact match, deliberately, and not the <c>&gt;=</c> the trial-balance reports use.</b>
/// <see cref="Accounting.Domain.ValueObjects.DocLife"/> is an ordinal enum and the reports
/// legitimately ask for "at least this degree of finality"; a کارتابل tab asks the opposite
/// question — "which vouchers are sitting in *this* state right now" — so a document that has
/// moved on must leave the tab it came from. Both readings are correct for their own caller;
/// do not unify them.
/// </param>
public sealed record VoucherHeadFilter(
    string? Year = null,
    string? DocNumFrom = null,
    string? DocNumTo = null,
    string? DateDocFrom = null,
    string? DateDocTo = null,
    Guid? SystemTypeId = null,
    DocLife? DocLife = null);
