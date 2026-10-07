using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// صندوق «دریافت اسناد از سایر سیستم‌ها» — سرسند/ردیف موقت (<c>TB_TMP_VOUCHERHEAD</c>/<c>TB_TMP_VOUCHERSDETAIL</c>)
/// که سیستم‌هایی مثل حقوق مستقیم در دیتابیس می‌نویسند. <c>VOUCHERSHEAD_ID</c> خالی = هنوز دریافت نشده.
/// </summary>
public interface ISystemVoucherInboxRepository
{
    Task<IReadOnlyList<SystemVoucherInboxRow>> ListAsync(string vahedCode, string? year, bool includeReceived, CancellationToken ct);

    /// <summary>سرسند موقت قابل‌ردگیری (برای ثبت <c>VOUCHERSHEAD_ID</c>) با ردیف‌هایش؛ فقط واحد خودش.</summary>
    Task<(TB_TMP_VOUCHERHEAD Head, IReadOnlyList<TB_TMP_VOUCHERSDETAIL> Lines)?> GetForReceiveAsync(Guid id, string vahedCode, CancellationToken ct);

    /// <summary>کد معین ← حساب معین فعال.</summary>
    Task<IReadOnlyDictionary<string, (Guid Id, string Name)>> ResolveMoinsAsync(IReadOnlyCollection<string> codes, CancellationToken ct);

    /// <summary>کد تفصیلی ← تفصیلی فعال قابل‌دید برای واحد (اولویت با تفصیلی خود واحد).</summary>
    Task<IReadOnlyDictionary<string, (Guid Id, string Name)>> ResolveTafsilisAsync(IReadOnlyCollection<string> codes, string vahedCode, CancellationToken ct);

    /// <summary>شمارهٔ سطح (۱..۷) ← شناسهٔ سطح تفصیلی فعال.</summary>
    Task<IReadOnlyDictionary<int, Guid>> GetLevelIdsByCodeAsync(CancellationToken ct);
}

public sealed record SystemVoucherInboxRow(
    Guid Id,
    string? SysType,
    string? DateDoc,
    string? Year,
    string? HeadDesc,
    DateTime? CreatedDate,
    int LineCount,
    decimal Debtor,
    decimal Creditor,
    Guid? VoucherHeadId,
    string? VoucherDocNum);
