using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// فیلتر واحد گزارش‌های چندواحدی (<c>IReportUnitScope</c>). Oracle بیش از ۱۰۰۰ عضو در یک <c>IN</c> نمی‌پذیرد
/// (ORA-01795)، پس فهرست در تکه‌های ۱۰۰۰تایی با <c>OR</c> می‌آید. در LINQ کدها با <see cref="EF.Constant{T}"/>
/// در SQL نوشته می‌شوند تا پارامتر مجموعه‌ای (که این نسخهٔ Oracle پشتیبانی نمی‌کند) ساخته نشود.
/// </summary>
internal static class ReportUnitScopeSql
{
    private const int Chunk = 1000;

    /// <summary>
    /// <c>(column IN (:pfx0,…) OR column IN (…))</c> با پارامترهای bind؛ کدها از سرور (فهرست واحدها) می‌آیند.
    /// </summary>
    public static (string Sql, IReadOnlyList<OracleParameter> Parameters) InClause(string column, IReadOnlyList<string> codes, string prefix)
    {
        var parts = new List<string>();
        var parameters = new List<OracleParameter>();
        for (var start = 0; start < codes.Count; start += Chunk)
        {
            var names = new List<string>();
            for (var i = start; i < Math.Min(start + Chunk, codes.Count); i++)
            {
                var name = $"{prefix}{i}";
                names.Add(":" + name);
                parameters.Add(new OracleParameter { ParameterName = name, OracleDbType = OracleDbType.Varchar2, Value = codes[i] });
            }

            parts.Add($"{column} IN ({string.Join(", ", names)})");
        }

        return ($"({string.Join(" OR ", parts)})", parameters);
    }

    /// <summary>واحد جاری، یا فهرست واحدهای دامنهٔ گزارش اگر پر باشد.</summary>
    public static IQueryable<T> WhereVahed<T>(
        this IQueryable<T> source, Expression<Func<T, string?>> selector, string vahedCode, IReadOnlyList<string>? codes)
    {
        if (codes is null || codes.Count == 0)
        {
            var eq = Expression.Equal(selector.Body, Expression.Constant(vahedCode, typeof(string)));
            return source.Where(Expression.Lambda<Func<T, bool>>(eq, selector.Parameters));
        }

        Expression? body = null;
        var constant = typeof(EF).GetMethod(nameof(EF.Constant))!.MakeGenericMethod(typeof(List<string>));
        var contains = typeof(List<string>).GetMethod(nameof(List<string>.Contains), [typeof(string)])!;
        for (var start = 0; start < codes.Count; start += Chunk)
        {
            var chunk = codes.Skip(start).Take(Chunk).ToList();
            var inlined = Expression.Call(constant, Expression.Constant(chunk));
            var call = Expression.Call(inlined, contains, selector.Body);
            body = body is null ? call : Expression.OrElse(body, call);
        }

        return source.Where(Expression.Lambda<Func<T, bool>>(body!, selector.Parameters));
    }
}
