using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Queries.GetBankAccountBalance;

/// <summary>
/// <c>GET api/treasury/bank-accounts/{id}/balance</c> — used by the UI for "موجودی فعلی/پس از
/// انتقال". Scoped to the CURRENT شمسی year (server-computed, not caller-supplied) — same year
/// انتقال <c>approve</c>'s own blocking balance check would use if <c>TRANSFER_DATE</c> happened to
/// be today (documented simplification, owner decision ۲۰۲۶-۰۹-۲۹).
/// </summary>
public sealed record GetBankAccountBalanceQuery(Guid Id) : IRequest<BankAccountBalanceDto?>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
