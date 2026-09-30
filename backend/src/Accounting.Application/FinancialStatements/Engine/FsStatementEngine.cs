using Accounting.Domain.ValueObjects;

namespace Accounting.Application.FinancialStatements.Engine;

/// <summary>کلید ستون‌های محاسبه‌شدهٔ یک اجرا.</summary>
public static class FsColumns
{
    /// <summary>دورهٔ جاری.</summary>
    public const string Current = "CUR";

    /// <summary>همان دوره در سال قبل — هدف <c>PRIOR(x)</c> و <c>x.PRV</c>.</summary>
    public const string Prior = "PRV";
}

/// <summary>
/// ماندهٔ یک معین در یک ستون. «ابتدا» = سند افتتاحیهٔ همان سال + اسناد پیش از شروع دوره؛ «دوره» =
/// بقیهٔ اسناد تا پایان دوره (سند اختتامیه هرگز). <c>docs/fs-module.md</c> §۷.
/// </summary>
public sealed record FsAccountBalance(
    string AccCode,
    string? AccName,
    decimal OpeningDebtor,
    decimal OpeningCreditor,
    decimal PeriodDebtor,
    decimal PeriodCreditor,
    string? VahedCode = null);

public sealed record FsEngineRow(
    string Code,
    int OrderNo,
    FsRowType RowType,
    string? Selector,
    FsValueType? ValueType,
    string? Formula);

public sealed record FsEngineStatement(string TemplateCode, IReadOnlyList<FsEngineRow> Rows);

/// <summary>نتیجهٔ یک ردیف در یک ستون: مبلغ (بدهکار مثبت) و، برای ردیف حساب، سهم هر معین.</summary>
public sealed record FsRowValue(decimal? Amount, IReadOnlyDictionary<string, decimal>? Accounts);

/// <summary>خطای اجرای موتور (ارجاع دوری بین صورت‌ها، صورت ارجاع‌شدهٔ غایب) — پیام فارسی.</summary>
public sealed class FsEngineException : Exception
{
    public FsEngineException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// موتور محاسبهٔ صورت‌های مالی (سند منبع §۷) — خالص، بدون وابستگی به دیتابیس. ورودی: ردیف‌های همهٔ
/// صورت‌های یک اجرا + ماندهٔ معین‌ها به‌ازای هر ستون. خروجی: مبلغ هر ردیف در هر ستون.
/// <list type="bullet">
/// <item><b>علامت:</b> همه‌چیز بدهکار-مثبت؛ ماهیت ردیف فقط در نمایش اعمال می‌شود.</item>
/// <item><b>حساب:</b> جمع معین‌هایی که انتخاب‌گر انتخابشان می‌کند، با <c>VALUE_TYPE</c>؛ جزء
/// <c>[D]</c>/<c>[C]</c> معین را فقط وقتی مقدارش مثبت/منفی است حساب می‌کند.</item>
/// <item><b>فرمول:</b> ارزیابی تنبل با حافظه؛ <c>STMT</c> صورت دیگر همین اجرا را می‌خواند؛ دور بین
/// صورت‌ها ⇒ <see cref="FsEngineException"/>. تقسیم بر صفر = صفر.</item>
/// <item><b>مقدار دستی:</b> از <c>external</c> (مقادیر دستی اجرا، بخش ۴۵-ه، به علامت حسابداری)؛ نبودش = صفر.</item>
/// </list>
/// محاسبه بی‌گرد است؛ گرد کردن فقط در نمایش.
/// </summary>
public sealed class FsStatementEngine
{
    private readonly Dictionary<string, Statement> _statements;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<FsAccountBalance>> _balances;
    private readonly Dictionary<(string Stmt, string Row, string Col), FsRowValue> _memo = new();
    private readonly HashSet<(string Stmt, string Row, string Col)> _visiting = new();
    private readonly IReadOnlyDictionary<(string Stmt, string Row, string Col), decimal> _external;

    private FsStatementEngine(
        IEnumerable<FsEngineStatement> statements,
        IReadOnlyDictionary<string, IReadOnlyList<FsAccountBalance>> balances,
        IReadOnlyDictionary<(string Stmt, string Row, string Col), decimal>? external)
    {
        _external = external ?? new Dictionary<(string, string, string), decimal>();
        _statements = statements.ToDictionary(s => s.TemplateCode, s => new Statement(s), StringComparer.Ordinal);
        _balances = balances;
    }

    /// <summary>
    /// همهٔ ردیف‌های همهٔ صورت‌ها را در همهٔ ستون‌های <paramref name="balances"/> محاسبه می‌کند.
    /// کلید نتیجه: (کد قالب، کد ردیف) ⇒ (کلید ستون ⇒ مقدار).
    /// </summary>
    /// <exception cref="FsEngineException">دور بین صورت‌ها یا ارجاع به صورتی که در اجرا نیست.</exception>
    public static IReadOnlyDictionary<(string Stmt, string Row), IReadOnlyDictionary<string, FsRowValue>> Compute(
        IReadOnlyList<FsEngineStatement> statements,
        IReadOnlyDictionary<string, IReadOnlyList<FsAccountBalance>> balances,
        IReadOnlyDictionary<(string Stmt, string Row, string Col), decimal>? external = null)
    {
        var engine = new FsStatementEngine(statements, balances, external);
        var result = new Dictionary<(string, string), IReadOnlyDictionary<string, FsRowValue>>();

        foreach (var s in statements)
        {
            foreach (var r in s.Rows)
            {
                var byCol = new Dictionary<string, FsRowValue>(StringComparer.Ordinal);

                foreach (var col in balances.Keys)
                {
                    byCol[col] = engine.Evaluate(s.TemplateCode, r.Code, col);
                }

                result[(s.TemplateCode, r.Code)] = byCol;
            }
        }

        return result;
    }

    /// <summary>مقدار یک معین (بدهکار مثبت) برای <paramref name="valueType"/> — همان قاعدهٔ ردیف «حساب».</summary>
    public static decimal AmountOf(FsAccountBalance b, FsValueType? valueType) => valueType switch
    {
        FsValueType.Opening => b.OpeningDebtor - b.OpeningCreditor,
        FsValueType.Movement => b.PeriodDebtor - b.PeriodCreditor,
        FsValueType.Debit => b.PeriodDebtor,
        FsValueType.Credit => -b.PeriodCreditor,
        _ => b.OpeningDebtor + b.PeriodDebtor - b.OpeningCreditor - b.PeriodCreditor,
    };

    private static bool IsValueRow(FsRowType t) => t is FsRowType.Account or FsRowType.Formula or FsRowType.External;

    private FsRowValue Evaluate(string stmtCode, string rowCode, string col)
    {
        var key = (stmtCode, rowCode, col);

        if (_memo.TryGetValue(key, out var cached))
        {
            return cached;
        }

        if (!_statements.TryGetValue(stmtCode, out var stmt))
        {
            throw new FsEngineException($"صورت «{stmtCode}» در این اجرا نیست.");
        }

        if (!stmt.Rows.TryGetValue(rowCode, out var row))
        {
            throw new FsEngineException($"ردیف «{rowCode}» در صورت «{stmtCode}» وجود ندارد.");
        }

        if (!_visiting.Add(key))
        {
            throw new FsEngineException($"ارجاع دوری در محاسبهٔ ردیف «{rowCode}» صورت «{stmtCode}».");
        }

        var value = row.Row.RowType switch
        {
            FsRowType.Account => EvaluateAccount(row, col),
            FsRowType.Formula => new FsRowValue(EvaluateExpr(stmt, row.Formula!.Root, col), null),
            FsRowType.External => new FsRowValue(_external.GetValueOrDefault((stmtCode, rowCode, col)), null),
            _ => new FsRowValue(null, null),
        };

        _visiting.Remove(key);
        _memo[key] = value;
        return value;
    }

    private FsRowValue EvaluateAccount(StatementRow row, string col)
    {
        if (!_balances.TryGetValue(col, out var balances))
        {
            return new FsRowValue(0m, new Dictionary<string, decimal>());
        }

        var accounts = new Dictionary<string, decimal>(StringComparer.Ordinal);
        var total = 0m;

        foreach (var b in balances)
        {
            var term = row.Selector!.Match(b.AccCode);

            if (term is null)
            {
                continue;
            }

            var amount = AmountOf(b, row.Row.ValueType);

            if ((term.Side == SelectorBalanceSide.DebitOnly && amount <= 0)
                || (term.Side == SelectorBalanceSide.CreditOnly && amount >= 0)
                || amount == 0)
            {
                continue;
            }

            accounts[b.AccCode] = accounts.GetValueOrDefault(b.AccCode) + amount;
            total += amount;
        }

        return new FsRowValue(total, accounts);
    }

    private decimal RowAmount(string stmtCode, string rowCode, string col)
        => _balances.ContainsKey(col) ? Evaluate(stmtCode, rowCode, col).Amount ?? 0m : 0m;

    private decimal EvaluateExpr(Statement stmt, FsExpr e, string col)
    {
        switch (e)
        {
            case FsNumberExpr n:
                return n.Value;

            case FsRowRefExpr r:
                return RowAmount(stmt.Code, r.RowCode, r.ColumnKey ?? col);

            case FsPriorExpr p:
                return RowAmount(stmt.Code, p.RowCode, FsColumns.Prior);

            case FsStatementRefExpr s:
                if (!_statements.ContainsKey(s.TemplateCode))
                {
                    throw new FsEngineException(
                        $"صورت «{stmt.Code}» به صورت «{s.TemplateCode}» ارجاع می‌دهد که در این اجرا نیست (نسخهٔ فعال ندارد؟).");
                }

                return RowAmount(s.TemplateCode, s.RowCode, col);

            case FsSumRangeExpr sum:
            {
                var from = stmt.Rows[sum.FromRowCode].Index;
                var to = stmt.Rows[sum.ToRowCode].Index;
                var total = 0m;

                for (var i = from; i <= to; i++)
                {
                    var r = stmt.Ordered[i];

                    if (IsValueRow(r.RowType))
                    {
                        total += RowAmount(stmt.Code, r.Code, col);
                    }
                }

                return total;
            }

            case FsUnaryExpr u:
                return -EvaluateExpr(stmt, u.Operand, col);

            case FsBinaryExpr b:
            {
                var l = EvaluateExpr(stmt, b.Left, col);
                var r = EvaluateExpr(stmt, b.Right, col);
                return b.Op switch
                {
                    '+' => l + r,
                    '-' => l - r,
                    '*' => l * r,
                    '/' => r == 0 ? 0 : l / r,
                    _ => throw new FsEngineException($"عملگر ناشناخته «{b.Op}»."),
                };
            }

            case FsFunctionExpr { Name: "ABS" } f:
                return Math.Abs(EvaluateExpr(stmt, f.Args[0], col));

            case FsFunctionExpr { Name: "ROUND" } f:
                return Math.Round(EvaluateExpr(stmt, f.Args[0], col), (int)((FsNumberExpr)f.Args[1]).Value, MidpointRounding.AwayFromZero);

            case FsIfExpr i:
            {
                var l = EvaluateExpr(stmt, i.CompareLeft, col);
                var r = EvaluateExpr(stmt, i.CompareRight, col);
                var ok = i.CompareOp switch
                {
                    "=" => l == r,
                    "<>" => l != r,
                    "<" => l < r,
                    "<=" => l <= r,
                    ">" => l > r,
                    ">=" => l >= r,
                    _ => false,
                };
                return EvaluateExpr(stmt, ok ? i.Then : i.Else, col);
            }

            default:
                throw new FsEngineException("عبارت فرمول پشتیبانی نمی‌شود.");
        }
    }

    private sealed class Statement
    {
        public Statement(FsEngineStatement s)
        {
            Code = s.TemplateCode;
            Ordered = s.Rows.OrderBy(r => r.OrderNo).ToList();
            Rows = Ordered
                .Select((r, i) => new StatementRow(
                    r,
                    i,
                    r.RowType == FsRowType.Account ? AccountSelector.Parse(r.Selector) : null,
                    r.RowType == FsRowType.Formula ? FsFormula.Parse(r.Formula) : null))
                .ToDictionary(r => r.Row.Code, StringComparer.Ordinal);
        }

        public string Code { get; }

        public List<FsEngineRow> Ordered { get; }

        public Dictionary<string, StatementRow> Rows { get; }
    }

    private sealed record StatementRow(FsEngineRow Row, int Index, AccountSelector? Selector, FsFormula? Formula);
}
