using Accounting.Application.Common.Interfaces;
using Accounting.Application.Reports.TrialBalance;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="ITrialBalanceReadRepository"/>. The one
/// read-repository in this codebase that does NOT use plain LINQ against <see cref="LegacyDbContext"/>.
///
/// <b>Why raw SQL.</b> <c>TB_ACCOUNTCODE.TYPECODE</c> and <c>TB_VOUCHERSHEAD.DOCLIFE</c> are Oracle
/// <c>NUMBER(1)</c> columns but are mapped to <c>bool?</c> on <see cref="Domain.Entity.TB_ACCOUNTCODE"/>/
/// <see cref="Domain.Entity.TB_VOUCHERSHEAD"/> (a known, out-of-scope-to-fix-here bug tracked in
/// <c>CLAUDE.md</c> "فاز ۱۲" — risk #2). This report needs <c>a.TYPECODE = 3</c> (select only
/// معین-level lines) and <c>h.DOCLIFE &gt;= :docLife</c> (an inclusive lower bound on a
/// 0..4-valued life-cycle status), both of which are inexpressible through EF LINQ against a
/// <c>bool?</c> property — there is no way to ask LINQ for "the underlying number is 3" when the
/// CLR type has already collapsed to true/false/null. Parameterized raw SQL reads these two
/// columns as numbers directly, sidestepping the bug without touching the write model, the entity,
/// or <c>LegacyDbContext</c>. This also matches how the reference project (<c>D:\CentralAccount</c>)
/// implements these exact three reports — hand-written SQL with explicit <c>OracleParameter</c>
/// binds for the same two columns.
///
/// <b>Why <c>Database.SqlQueryRaw&lt;T&gt;</c> and not Dapper.</b> This project has no Dapper
/// dependency anywhere and no raw SQL anywhere else; adding a new package for a single query is not
/// justified when EF Core's own ad-hoc <c>SqlQueryRaw&lt;T&gt;</c> (materializing into
/// <see cref="TrialBalanceAggregateRow"/> via public settable properties matched by column alias
/// name) covers this exactly. No keyless entity, no <c>ToView</c>/<c>HasNoKey</c> registration was
/// added to <see cref="LegacyDbContext"/> — none is required for this API.
///
/// <b>The Total = Opening + Period invariant, and why it holds unconditionally.</b> The SQL below
/// defines three window predicates:
/// <list type="bullet">
/// <item><description><c>{T}</c> (cumulative/"Total"): <c>:toDate IS NULL OR (h.DATE_DOC IS NOT
/// NULL AND h.DATE_DOC &lt;= :toDate)</c>.</description></item>
/// <item><description><c>{O}</c> ("Opening"): <c>:fromDate IS NOT NULL AND h.DATE_DOC IS NOT NULL
/// AND h.DATE_DOC &lt; :fromDate</c>.</description></item>
/// <item><description><c>{P}</c> ("Period"): literally <c>({T}) AND NOT ({O})</c> — spelled out as
/// that exact conjunction in the SQL text, never as an independently-derived condition.</description></item>
/// </list>
/// Because <c>{P}</c> is defined in terms of <c>{T}</c> and <c>{O}</c> rather than restated, the
/// identity <c>TotalDebtor == OpeningDebtor + PeriodDebtor</c> (and the same for Creditor) is true
/// <b>by construction</b> for every row — including rows whose <c>DATE_DOC</c> is
/// <see langword="null"/>. For such a row, <c>{T}</c> only evaluates true when <c>ToDate</c> is
/// itself <see langword="null"/> (no upper bound requested at all); <c>{O}</c> is always false when
/// <c>DATE_DOC</c> is null (it requires <c>DATE_DOC IS NOT NULL</c>); so <c>{P}</c> collapses to
/// exactly <c>{T}</c>, and the three-way split still partitions the row correctly into either
/// "period" or "excluded", never double-counting and never leaving a silent remainder. This is the
/// single most important correctness property of this query — do not "simplify" <c>{P}</c> into an
/// independently-spelled-out condition without re-deriving this proof.
///
/// <b><c>DATE_DOC</c> comparison.</b> <c>TB_VOUCHERSHEAD.DATE_DOC</c> is <c>VARCHAR2(8)</c> — a
/// zero-padded Jalali <c>YYYYMMDD</c> string, e.g. <c>"14050101"</c>. Ordinary lexicographic string
/// comparison (<c>&lt;=</c>, <c>&lt;</c>) is correct for that fixed-width, zero-padded format — it
/// agrees with numeric/chronological ordering digit-by-digit. <see cref="GetAggregatesAsync"/>
/// binds <c>fromDate</c>/<c>toDate</c> as plain Oracle <c>VARCHAR2</c> strings, never as
/// <c>DATE</c>, precisely so this stays a string comparison and never triggers an implicit
/// string-to-date conversion (which would fail or silently misbehave against this format).
///
/// <b>Deliberate difference from the reference project — two bugs fixed here.</b>
/// <list type="number">
/// <item><description>The reference applies <c>h.isdeleted = 0</c> only in its moin/kol report
/// modes, omitting it for group/tafsili — so soft-deleted vouchers leak into those levels' totals.
/// This query applies <c>(h.ISDELETED IS NULL OR h.ISDELETED = 0)</c> uniformly in the
/// level-independent <c>WHERE</c> clause, at every level.</description></item>
/// <item><description>The reference never filters <c>d.isdeleted</c> at all. This query does —
/// critical for this codebase specifically, because
/// <c>DeleteVoucherDetailCommand</c> (phase 10) soft-deletes an individual line without touching
/// its head, so a head-only <c>ISDELETED</c> filter would silently include deleted lines.</description></item>
/// </list>
///
/// <b>Why the ancestor joins (<c>ak</c>/<c>ag</c>) are <c>LEFT JOIN</c>, not <c>JOIN</c>.</b>
/// (Found by <c>/code-review</c> before this phase's first commit — not a hypothetical.) This
/// project explicitly abandoned the fixed 3-level Group/Kol/Moin hierarchy invariant (see
/// CLAUDE.md "تصمیم معماری دوم"), so a Moin account's <c>PARENTID</c> chain is not guaranteed to
/// resolve all the way up. With a plain <c>JOIN</c>, any voucher line posted against a Moin account
/// whose Kol or Group ancestor is missing/dangling would be silently dropped from the result set —
/// including from the <b>Moin-level</b> report, which structurally needs only the <c>a</c> alias
/// and none of <c>ak</c>/<c>ag</c>. A silently missing row understates one side of the report and
/// can break the "total debtor = total credit" property this class otherwise goes out of its way to
/// protect (see the ISDELETED remarks below). With <c>LEFT JOIN</c>: Moin-level reports are
/// unaffected by a broken ancestor chain (they never read <c>ak</c>/<c>ag</c>); Group/Kol-level
/// reports fall back to <c>NULL</c> for <see cref="ColumnExprFor"/>'s code/name expression for such
/// a row — grouped into a single visible "unknown ancestor" bucket by Oracle's GROUP BY (NULL is
/// one group), which surfaces the data problem instead of hiding it. This was not re-verified
/// against real Oracle (no live connection in this task) — flagged as an open item.
///
/// <b>Deliberate omission — account-code rows are NOT filtered by <c>ISDELETED</c>.</b> <c>a</c>/
/// <c>ak</c>/<c>ag</c> (the معین/کل/گروه <c>TB_ACCOUNTCODE</c> joins) carry no <c>ISDELETED</c>
/// predicate. A soft-deleted account code that still carries historical voucher lines must remain
/// in the trial balance — excluding it would silently understate one side of the report and break
/// the fundamental invariant that total debits equal total credits across the whole report. This is
/// not an oversight; adding that filter later would be a regression.
///
/// <b><c>{codeExpr}</c>/<c>{nameExpr}</c> — SQL injection posture.</b> These two column expressions
/// are chosen by <see cref="ColumnExprFor"/>, a hardcoded C# <c>switch</c> over the
/// <see cref="TrialBalanceLevel"/> enum. They are NEVER interpolated from caller-supplied input —
/// the enum is validated by <c>GetTrialBalance4QueryValidator</c>/<c>...6.../...8...</c>
/// (<c>.IsInEnum()</c>) before this method is ever reached, and even if it were not, the switch's
/// default branch throws rather than falling through to an unexpected string. There is no code path
/// by which client-controlled text becomes part of the SQL text itself; every other value in this
/// query is bound as an <see cref="OracleParameter"/>.
/// </summary>
public sealed class TrialBalanceReadRepository : ITrialBalanceReadRepository
{
    /// <summary>
    /// Cumulative ("Total", <c>{T}</c>) window predicate — see the class remarks for the full
    /// algebra. <c>:toDate</c> is reused verbatim by name in three places in the generated SQL,
    /// relying on Oracle's <c>BindByName</c> binding so that a single <see cref="OracleParameter"/>
    /// named <c>toDate</c> satisfies every occurrence, regardless of how many times it appears in
    /// the text — this is NOT an assumption: decompiling the exact package version this project
    /// references (<c>Oracle.EntityFrameworkCore</c> 10.23.26000, see
    /// <c>Accounting.Infrastructure.csproj</c>) shows
    /// <c>OracleRelationalCommand.CreateDbCommand</c> — the single command-creation path EF Core's
    /// relational pipeline uses for every <see cref="DbCommand"/> it builds, including
    /// <c>Database.SqlQueryRaw&lt;T&gt;</c> — sets <c>((OracleCommand)dbCommand).BindByName = true</c>
    /// unconditionally, before any parameter is ever added. If this provider version is ever
    /// upgraded, re-verify this by decompiling <c>OracleRelationalCommand.CreateDbCommand</c> again
    /// (e.g. with <c>ilspycmd</c>) rather than re-assuming it holds.
    /// </summary>
    private const string TotalWindowPredicate =
        "(:toDate IS NULL OR (h.DATE_DOC IS NOT NULL AND h.DATE_DOC <= :toDate))";

    /// <summary>
    /// Opening ("<c>{O}</c>") window predicate — see the class remarks. Same named-parameter reuse
    /// note as <see cref="TotalWindowPredicate"/>, for <c>:fromDate</c>.
    /// </summary>
    private const string OpeningWindowPredicate =
        "(:fromDate IS NOT NULL AND h.DATE_DOC IS NOT NULL AND h.DATE_DOC < :fromDate)";

    private readonly LegacyDbContext _dbContext;

    public TrialBalanceReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<TrialBalanceAggregateRow>> GetAggregatesAsync(
        TrialBalanceLevel level,
        string year,
        string? fromDate,
        string? toDate,
        string? vahedCode,
        int? docLife,
        CancellationToken cancellationToken = default)
    {
        var (codeExpr, nameExpr) = ColumnExprFor(level);

        // {P} = {T} AND NOT {O}, spelled out as that literal conjunction — see the class remarks
        // for why this exact construction (not an independently-derived condition) is what makes
        // Total = Opening + Period true unconditionally.
        var periodPredicate = $"({TotalWindowPredicate}) AND NOT ({OpeningWindowPredicate})";

        var sql = $"""
            SELECT
              {codeExpr}      AS "Code",
              MAX({nameExpr}) AS "Description",
              NVL(SUM(CASE WHEN {periodPredicate} THEN d.DEBTOR   END), 0) AS "PeriodDebtor",
              NVL(SUM(CASE WHEN {periodPredicate} THEN d.CREDITOR END), 0) AS "PeriodCreditor",
              NVL(SUM(CASE WHEN {OpeningWindowPredicate} THEN d.DEBTOR   END), 0) AS "OpeningDebtor",
              NVL(SUM(CASE WHEN {OpeningWindowPredicate} THEN d.CREDITOR END), 0) AS "OpeningCreditor",
              NVL(SUM(CASE WHEN {TotalWindowPredicate} THEN d.DEBTOR   END), 0) AS "TotalDebtor",
              NVL(SUM(CASE WHEN {TotalWindowPredicate} THEN d.CREDITOR END), 0) AS "TotalCreditor"
            FROM TB_VOUCHERSDETAIL d
            JOIN TB_VOUCHERSHEAD  h  ON h.ID  = d.VOUCHERSHEAD_ID
            JOIN TB_ACCOUNTCODE   a  ON a.ID  = d.ACCOUNT_ID
            LEFT JOIN TB_ACCOUNTCODE ak ON ak.ID = a.PARENTID
            LEFT JOIN TB_ACCOUNTCODE ag ON ag.ID = ak.PARENTID
            WHERE a.TYPECODE = 3
              AND h.YEAR = :year
              AND (h.ISDELETED IS NULL OR h.ISDELETED = 0)
              AND (d.ISDELETED IS NULL OR d.ISDELETED = 0)
              AND (:vahedCode IS NULL OR h.VAHEDCODE = :vahedCode)
              AND (:docLife   IS NULL OR h.DOCLIFE  >= :docLife)
            GROUP BY {codeExpr}
            ORDER BY {codeExpr}
            """;

        var parameters = new OracleParameter[]
        {
            new() { ParameterName = "year", OracleDbType = OracleDbType.Char, Value = year },
            new() { ParameterName = "vahedCode", OracleDbType = OracleDbType.Varchar2, Value = (object?)vahedCode ?? DBNull.Value },
            new() { ParameterName = "docLife", OracleDbType = OracleDbType.Int32, Value = (object?)docLife ?? DBNull.Value },
            new() { ParameterName = "fromDate", OracleDbType = OracleDbType.Varchar2, Value = (object?)fromDate ?? DBNull.Value },
            new() { ParameterName = "toDate", OracleDbType = OracleDbType.Varchar2, Value = (object?)toDate ?? DBNull.Value },
        };

        var rows = await _dbContext.Database
            .SqlQueryRaw<TrialBalanceAggregateRow>(sql, parameters)
            .ToListAsync(cancellationToken);

        return rows;
    }

    /// <summary>
    /// Hardcoded mapping from <see cref="TrialBalanceLevel"/> to the <c>ACCCODE</c>/<c>ACCCODENAME</c>
    /// column pair at that level of the <c>TB_ACCOUNTCODE</c> self-join chain
    /// (<c>a</c> = معین, <c>ak</c> = کل, <c>ag</c> = گروه). See the class remarks for why this is
    /// safe against SQL injection despite being interpolated into the query text.
    /// </summary>
    private static (string CodeExpr, string NameExpr) ColumnExprFor(TrialBalanceLevel level) => level switch
    {
        TrialBalanceLevel.Group => ("ag.ACCCODE", "ag.ACCCODENAME"),
        TrialBalanceLevel.Kol => ("ak.ACCCODE", "ak.ACCCODENAME"),
        TrialBalanceLevel.Moin => ("a.ACCCODE", "a.ACCCODENAME"),
        _ => throw new ArgumentOutOfRangeException(
            nameof(level), level, "Unsupported trial balance level."),
    };
}
