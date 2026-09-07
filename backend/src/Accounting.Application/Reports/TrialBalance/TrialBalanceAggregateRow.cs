namespace Accounting.Application.Reports.TrialBalance;

/// <summary>
/// Materialization target of <see cref="Accounting.Application.Common.Interfaces.ITrialBalanceReadRepository"/>'s
/// raw SQL aggregate query. This is an internal implementation detail shared only between the
/// repository and the three Query handlers in this folder — it is never returned directly from a
/// Controller (each handler projects it into the public-facing
/// <see cref="TrialBalance4RowDto"/>/<see cref="TrialBalance6RowDto"/>/<see cref="TrialBalance8RowDto"/>
/// instead). It must stay <see langword="public"/> (not <see langword="internal"/>) purely because
/// <c>Accounting.Infrastructure</c> — a separate assembly, with no <c>InternalsVisibleTo</c> grant
/// — both implements the interface that returns it and is the type EF Core's
/// <c>Database.SqlQueryRaw&lt;T&gt;</c> materializes into via reflection.
///
/// Property names are matched positionally-by-name (case-insensitive) against the SQL column
/// aliases in <c>TrialBalanceReadRepository</c>'s query text — they MUST stay in sync with the
/// <c>AS "Code"</c>/<c>AS "Description"</c>/... aliases there. This type intentionally has public
/// settable properties and an implicit public parameterless constructor (a plain mutable class,
/// not a positional record) because that is what EF Core's ad-hoc <c>SqlQueryRaw&lt;T&gt;</c>
/// materialization requires — it is not a mapped/keyless entity and needs no <c>ToView</c> or
/// <c>HasNoKey</c> registration in <c>LegacyDbContext</c>.
/// </summary>
public sealed class TrialBalanceAggregateRow
{
    /// <summary>The account code at the requested <see cref="TrialBalanceLevel"/> — either
    /// <c>ag.ACCCODE</c> (Group), <c>ak.ACCCODE</c> (Kol) or <c>a.ACCCODE</c> (Moin), chosen by
    /// the repository's hardcoded column-expression switch.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>The account name/title at the same level as <see cref="Code"/>. Wrapped in
    /// <c>MAX(...)</c> in the SQL purely because it is a non-aggregated column pulled through a
    /// <c>GROUP BY</c> — every row sharing the same <see cref="Code"/> necessarily shares the same
    /// name, so <c>MAX</c> is just the mechanism to satisfy Oracle's GROUP BY rules, not a
    /// meaningful aggregation.</summary>
    public string? Description { get; set; }

    /// <summary>Sum of <c>DEBTOR</c> for voucher lines whose head falls strictly inside the
    /// requested period window (<c>{P}</c> — see the repository's XML doc for the exact
    /// Total-minus-Opening algebra).</summary>
    public decimal PeriodDebtor { get; set; }

    /// <summary>Sum of <c>CREDITOR</c> for the same period window as <see cref="PeriodDebtor"/>.</summary>
    public decimal PeriodCreditor { get; set; }

    /// <summary>Sum of <c>DEBTOR</c> for voucher lines strictly before <c>FromDate</c> (<c>{O}</c>
    /// — the opening/carry-forward window). Zero whenever <c>FromDate</c> is not supplied.</summary>
    public decimal OpeningDebtor { get; set; }

    /// <summary>Sum of <c>CREDITOR</c> for the same opening window as <see cref="OpeningDebtor"/>.</summary>
    public decimal OpeningCreditor { get; set; }

    /// <summary>Sum of <c>DEBTOR</c> for voucher lines up to and including <c>ToDate</c>
    /// (<c>{T}</c> — the cumulative window). Always equal to
    /// <see cref="OpeningDebtor"/> + <see cref="PeriodDebtor"/> by construction, never merely by
    /// coincidence of the arithmetic — see the repository XML doc.</summary>
    public decimal TotalDebtor { get; set; }

    /// <summary>Sum of <c>CREDITOR</c> for the same cumulative window as <see cref="TotalDebtor"/>.</summary>
    public decimal TotalCreditor { get; set; }
}
