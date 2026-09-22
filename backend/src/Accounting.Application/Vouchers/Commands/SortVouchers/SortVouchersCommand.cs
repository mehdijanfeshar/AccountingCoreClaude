using Accounting.Application.Common.Security;
using MediatR;
using System.Text.Json.Serialization;

namespace Accounting.Application.Vouchers.Commands.SortVouchers;

/// <summary>
/// How a sort range is expressed. The two are mutually exclusive — the reference project's
/// <c>SortType</c>.
/// </summary>
public enum VoucherSortType
{
    /// <summary>بازه بر اساس شماره سند.</summary>
    DocNum = 1,

    /// <summary>بازه بر اساس تاریخ سند.</summary>
    DocDate = 2,
}

/// <summary>
/// مرتب‌سازی اسناد — renumbers a range of vouchers into date order, so that شماره سند runs in the
/// same sequence as تاریخ سند.
///
/// <para>
/// Ported from the reference project's <c>SortVoucherCommand</c> (<c>Cartable/SortVoucher</c>).
/// The range is given either as a شماره سند span or a تاریخ سند span; <c>YEAR</c> and
/// <c>VAHEDCODE</c> are never caller-supplied.
/// </para>
/// </summary>
/// <param name="SortType">Which pair of bounds below is in use.</param>
/// <param name="DocNumFrom">Inclusive lower bound, required when <see cref="SortType"/> is DocNum.</param>
/// <param name="DocNumTo">Inclusive upper bound, required when <see cref="SortType"/> is DocNum.</param>
/// <param name="DateDocFrom">Inclusive lower bound (<c>YYYYMMDD</c>), required when SortType is DocDate.</param>
/// <param name="DateDocTo">Inclusive upper bound (<c>YYYYMMDD</c>), required when SortType is DocDate.</param>
/// <param name="Year">سال مالی — a real parameter, unlike VahedCode.</param>
public sealed record SortVouchersCommand(
    VoucherSortType SortType,
    string? DocNumFrom,
    string? DocNumTo,
    string? DateDocFrom,
    string? DateDocTo,
    string Year) : IRequest<int>, IVahedScopedCommand
{
    /// <summary>
    /// Server-assigned by <c>VahedScopeBehavior</c>, never bound from the request body — the
    /// reference project took this from the caller.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
