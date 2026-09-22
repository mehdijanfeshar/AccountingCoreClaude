using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.BillLogs.Queries.GetBillLogById;

/// <summary>
/// Returns a single <c>TB_BILL_LOG</c> row projected to <see cref="BillLogDto"/>, or
/// <see langword="null"/> if no row with the given <see cref="Id"/> exists. No logical-delete
/// filter is applied.
/// </summary>
/// <param name="Id">ID column to look up.</param>
public sealed record GetBillLogByIdQuery(Guid Id) : IRequest<BillLogDto?>, IVahedScopedQuery
{
    /// <summary>
    /// Caller's own organizational unit, server-assigned by <c>VahedScopeBehavior</c> — never
    /// client input. Used to refuse a row belonging to another unit; see
    /// <c>VahedOwnership</c> and IDOR risk #1.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
