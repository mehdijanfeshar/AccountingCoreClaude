using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Common.Interfaces;

/// <summary>یک سطر خام هرم سنی: گردش یک (معین شناسه‌دار، شناسه) در یک سال مشخص.</summary>
public sealed record AgePyramidRawRow(
    Guid AccountId, string AccCode, string? AccName, AttribFlag? Flag, string Identifier, string Year,
    decimal Debtor, decimal Creditor);

public interface IAgePyramidReadRepository
{
    /// <summary>گردش هر (معین شناسه‌دار، شناسه، سال) روی تمام سال‌های موجود واحد — نه فقط یک سال.
    /// <paramref name="accountIds"/> خالی/null یعنی همهٔ معین‌های شناسه‌دار واحد.</summary>
    Task<IReadOnlyList<AgePyramidRawRow>> GetAllYearsAsync(
        string vahedCode, IReadOnlyCollection<Guid>? accountIds, CancellationToken cancellationToken = default);
}
