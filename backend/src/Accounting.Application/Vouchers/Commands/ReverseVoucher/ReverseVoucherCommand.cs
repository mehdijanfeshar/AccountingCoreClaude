using Accounting.Application.Common.Security;
using MediatR;
using System.Text.Json.Serialization;

namespace Accounting.Application.Vouchers.Commands.ReverseVoucher;

/// <summary>
/// معکوس سند — creates a NEW voucher whose lines mirror the source with بدهکار and بستانکار
/// swapped, leaving the source untouched.
///
/// <para>
/// Ported from the reference project's <c>AddReversVoucherCommand</c>
/// (<c>Cartable/AddReversVoucher</c>). Reversal is how an accountant undoes the effect of a
/// posted voucher without deleting it — which is exactly why it is the one write operation that
/// is allowed against a voucher in ANY state: the locked voucher is not modified, a new draft is
/// created beside it.
/// </para>
/// </summary>
/// <param name="Id">The voucher to reverse. Only read; never modified.</param>
public sealed record ReverseVoucherCommand(Guid Id) : IRequest<Guid>, IVahedScopedCommand
{
    /// <summary>
    /// Server-assigned by <c>VahedScopeBehavior</c>, never bound from the request body.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
