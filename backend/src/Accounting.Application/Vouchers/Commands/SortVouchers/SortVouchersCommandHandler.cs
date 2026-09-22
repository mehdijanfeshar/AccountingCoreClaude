using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.Vouchers.Commands.SortVouchers;

/// <summary>
/// Renumbers the selected vouchers into تاریخ سند order. Returns how many were renumbered.
///
/// <para>
/// <b>Why this is a two-pass write.</b> <c>UK_VOUCHERHEAD_NUMBER</c> is UNIQUE on
/// <c>(DOC_NUM, YEAR, VAHEDCODE)</c>, so assigning final numbers directly would collide with
/// numbers still held by rows not yet updated — renumbering 000101..000105 into a different order
/// hits the constraint on the very first row. Every voucher is therefore first parked on a
/// temporary number, flushed, and only then given its final one.
/// </para>
///
/// <para>
/// ⚠️ <b>The reference project's temporary number is unsafe and is NOT ported.</b> It uses
/// <c>DocNumber.Substring(1, 4) + "S"</c>, which discards the last digit: "000123" and "000124"
/// both become "0012S". Any two vouchers in a contiguous range collide, which is the normal case
/// for this operation. This implementation uses a per-row sequential token instead
/// (<see cref="TemporaryDocNum"/>), which cannot collide with another temporary value or with any
/// real number.
/// </para>
/// </summary>
public sealed class SortVouchersCommandHandler : IRequestHandler<SortVouchersCommand, int>
{
    private readonly IVoucherHeadRepository _voucherHeadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public SortVouchersCommandHandler(
        IVoucherHeadRepository voucherHeadRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _voucherHeadRepository = voucherHeadRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(SortVouchersCommand request, CancellationToken cancellationToken)
    {
        var all = await _voucherHeadRepository.GetActiveByYearAsync(
            request.VahedCode,
            request.Year,
            cancellationToken);

        var inRange = all.Where(v => IsInRange(v, request)).ToList();

        if (inRange.Count == 0)
        {
            return 0;
        }

        // Every voucher in the RANGE must still be changeable. The reference checks this against
        // the whole year rather than the range, which makes sorting impossible for the rest of the
        // year as soon as one voucher anywhere is finalized; the intent — "you cannot renumber a
        // finalized document" — is what is implemented here.
        foreach (var voucher in inRange)
        {
            VoucherEditability.EnsureEditable(voucher.ID, voucher.DOCLIFE);
        }

        // Numbering continues from the highest voucher that sits BELOW the range, so sorting a
        // slice does not renumber it on top of what precedes it.
        var startFrom = HighestNumberBelowRange(all, inRange);

        // Date order is the whole point of the operation. ID is the tie-breaker so two vouchers
        // on the same day get a stable, repeatable order instead of whatever the database returned.
        var ordered = inRange
            .OrderBy(v => v.DATE_DOC, StringComparer.Ordinal)
            .ThenBy(v => v.ID)
            .ToList();

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        // Pass 1 — park every row on a collision-free temporary number and flush, so none of the
        // final numbers below can hit a row that still holds it.
        for (var i = 0; i < ordered.Count; i++)
        {
            ordered[i].DOC_NUM = TemporaryDocNum(i);
            ordered[i].CHANGEUSERID = userId;
            ordered[i].UPDATEDDATE = now;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Pass 2 — the real numbers, in date order.
        for (var i = 0; i < ordered.Count; i++)
        {
            ordered[i].DOC_NUM = FormatDocNum(startFrom + i + 1);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ordered.Count;
    }

    private static bool IsInRange(TB_VOUCHERSHEAD voucher, SortVouchersCommand request)
    {
        if (request.SortType == VoucherSortType.DocNum)
        {
            return TryParseDocNum(voucher.DOC_NUM, out var number)
                && TryParseDocNum(request.DocNumFrom, out var from)
                && TryParseDocNum(request.DocNumTo, out var to)
                && number >= from
                && number <= to;
        }

        // DATE_DOC is a fixed-width YYYYMMDD string, so ordinal string comparison is exactly
        // chronological and needs no parsing.
        return voucher.DATE_DOC is not null
            && request.DateDocFrom is not null
            && request.DateDocTo is not null
            && string.CompareOrdinal(voucher.DATE_DOC, request.DateDocFrom) >= 0
            && string.CompareOrdinal(voucher.DATE_DOC, request.DateDocTo) <= 0;
    }

    /// <summary>
    /// The highest numeric <c>DOC_NUM</c> among vouchers outside and below the range — the point
    /// renumbering resumes from. Zero when the range starts at the beginning of the year.
    /// </summary>
    private static int HighestNumberBelowRange(
        IReadOnlyList<TB_VOUCHERSHEAD> all,
        IReadOnlyList<TB_VOUCHERSHEAD> inRange)
    {
        var lowestInRange = inRange
            .Select(v => TryParseDocNum(v.DOC_NUM, out var n) ? n : int.MaxValue)
            .Min();

        var inRangeIds = inRange.Select(v => v.ID).ToHashSet();

        var below = all
            .Where(v => !inRangeIds.Contains(v.ID))
            .Select(v => TryParseDocNum(v.DOC_NUM, out var n) ? n : -1)
            .Where(n => n >= 0 && n < lowestInRange)
            .ToList();

        return below.Count == 0 ? 0 : below.Max();
    }

    /// <summary>
    /// A temporary <c>DOC_NUM</c> that cannot collide: it starts with a letter, so it can never
    /// equal a real all-digit voucher number, and it carries the row index, so two temporaries are
    /// never equal either. Fits the 6-character column for ranges up to 99,999 vouchers.
    /// </summary>
    private static string TemporaryDocNum(int index) => $"S{index:00000}";

    private static string FormatDocNum(int value) => value.ToString("000000");

    private static bool TryParseDocNum(string? value, out int number)
    {
        number = 0;
        return !string.IsNullOrWhiteSpace(value) && int.TryParse(value, out number);
    }
}
