namespace Accounting.Application.Common.Search;

/// <summary>
/// Comparison operators a <see cref="SearchParam"/> may request. Ported from the project owner's
/// previous system so report filtering keeps a shape the team already knows.
///
/// <para>
/// <b>This is a closed enum, and that is load-bearing.</b> The operator reaches SQL as a fixed
/// token chosen by a <c>switch</c> over these values — never as text supplied by the caller. A
/// value outside the enum is rejected by validation before any SQL is built.
/// </para>
/// </summary>
public enum SearchOperator
{
    EQ = 0,
    NEQ = 1,
    GT = 2,
    LT = 3,
    GTE = 4,
    LTE = 5,
    LIKE = 6,
    IN = 7,
}

/// <summary>
/// One filter clause in a report request: which field, compared how, against what.
///
/// <para>
/// <b>Why the generic shape is safe here, and where the danger actually sits.</b> The owner's
/// previous system used Oracle bind parameters, and so does this one — a <see cref="Value"/> can
/// never be read as SQL. But binding protects values only: a column <i>name</i> cannot be a bind
/// variable, so <see cref="Property"/> would have to be concatenated into the statement, and that
/// is precisely where a generic filter turns into an injection point or a way to read columns the
/// endpoint never meant to expose.
/// </para>
///
/// <para>
/// So <see cref="Property"/> is never used as a column name. Each report declares a small
/// allowlist of logical field names (see <c>TrialBalanceSearchFields</c>) and maps each one to a
/// SQL expression it chose itself. An unknown name is a 400, not an ignored clause — silently
/// dropping a filter the caller asked for would return a wider report than requested while looking
/// like it worked.
/// </para>
/// </summary>
public sealed class SearchParam
{
    /// <summary>
    /// Logical field name, matched case-insensitively against the report's allowlist. Not a column
    /// name and never inserted into SQL.
    /// </summary>
    public string Property { get; set; } = string.Empty;

    public SearchOperator Operator { get; set; }

    /// <summary>
    /// The value to compare against, always bound as a parameter. For <see cref="SearchOperator.IN"/>
    /// this is a comma-separated list, and each item is bound individually.
    /// </summary>
    public string Value { get; set; } = string.Empty;
}
