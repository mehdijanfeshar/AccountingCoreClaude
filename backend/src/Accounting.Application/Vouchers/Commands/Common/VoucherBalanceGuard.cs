using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Vouchers.Commands.Common;

/// <summary>
/// کنترل تراز سند (ریسک #۳، تصمیم صاحب پروژه ۲۰۲۶-۱۰-۰۷): «یادداشت» می‌تواند ناتراز باشد؛
/// هر وضعیت بعد از آن (موقت، بررسی‌شده، تأیید دائم) جمع بدهکار = جمع بستانکار و دست‌کم یک
/// ردیف می‌خواهد. در مرز تغییر وضعیت اعمال می‌شود (ساخت، ویرایش سرسند، کارتابل)، نه روی
/// تک‌تک ذخیرهٔ ردیف — فرم سند ردیف‌ها را یکی‌یکی ذخیره می‌کند.
/// </summary>
public static class VoucherBalanceGuard
{
    public static bool RequiresBalance(DocLife? state) => state is not null and not DocLife.Draft;

    /// <summary>سندهای ذخیره‌شده — جمع از دیتابیس خوانده می‌شود.</summary>
    public static async Task EnsureBalancedAsync(
        IVoucherDetailRepository detailRepository,
        IReadOnlyCollection<TB_VOUCHERSHEAD> heads,
        CancellationToken cancellationToken)
    {
        if (heads.Count == 0)
        {
            return;
        }

        var totals = await detailRepository.GetTotalsByHeadsAsync(heads.Select(h => h.ID).ToList(), cancellationToken);

        foreach (var head in heads)
        {
            var t = totals.TryGetValue(head.ID, out var found) ? found : new VoucherTotals(0m, 0m, 0);
            Ensure(head.ID, head.DOC_NUM, t.Debtor, t.Creditor, t.LineCount);
        }
    }

    /// <summary>سند تازه که ردیف‌هایش هنوز ذخیره نشده‌اند.</summary>
    public static void EnsureBalanced(Guid headId, string? docNum, IEnumerable<(decimal? Debtor, decimal? Creditor)> lines)
    {
        var list = lines.ToList();
        Ensure(headId, docNum, list.Sum(l => l.Debtor ?? 0m), list.Sum(l => l.Creditor ?? 0m), list.Count);
    }

    private static void Ensure(Guid headId, string? docNum, decimal debtor, decimal creditor, int lineCount)
    {
        if (lineCount == 0 || debtor != creditor)
        {
            throw new VoucherUnbalancedException(headId, docNum, debtor, creditor, lineCount);
        }
    }
}
