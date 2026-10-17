using Accounting.Domain.Sales;

namespace Accounting.Application.Sales;

// ماژول فروش، فاز ۱ — طرح: docs/sales-module.md. واحد همیشه از IVahedScoped، هرگز از بدنه.

public interface ISalesRepository
{
    // ─── تنظیمات، نگاشت حساب، نرخ ارزش افزوده، نقش ───
    Task<SalesSetting?> GetSettingAsync(string vahedCode, bool tracking, CancellationToken ct);
    Task AddSettingAsync(SalesSetting setting, CancellationToken ct);
    Task<IReadOnlyList<SalesAccountMap>> GetAccountMapsAsync(string vahedCode, bool tracking, CancellationToken ct);
    void RemoveAccountMaps(IEnumerable<SalesAccountMap> maps);
    Task AddAccountMapAsync(SalesAccountMap map, CancellationToken ct);
    Task<IReadOnlyList<SalesVatRate>> ListVatRatesAsync(bool tracking, CancellationToken ct);
    Task AddVatRateAsync(SalesVatRate rate, CancellationToken ct);
    Task<IReadOnlyList<SalesUserRole>> ListRolesAsync(string vahedCode, string? userId, CancellationToken ct);
    Task<SalesUserRole?> GetRoleForUpdateAsync(Guid id, CancellationToken ct);
    Task AddRoleAsync(SalesUserRole role, CancellationToken ct);
    void RemoveRole(SalesUserRole role);

    // ─── کالا و مشتری ───
    Task<IReadOnlyList<SalesItem>> ListItemsAsync(string vahedCode, string? search, bool activeOnly, CancellationToken ct);
    Task<IReadOnlyDictionary<Guid, SalesItem>> GetItemsAsync(IReadOnlyCollection<Guid> ids, bool tracking, CancellationToken ct);
    Task<bool> ItemCodeExistsAsync(string vahedCode, string code, Guid? exceptId, CancellationToken ct);
    Task AddItemAsync(SalesItem item, CancellationToken ct);
    Task<IReadOnlyList<SalesCustomerRow>> ListCustomersAsync(string vahedCode, string? search, IReadOnlyCollection<Guid>? ids, CancellationToken ct);
    Task<SalesCustomer?> GetCustomerAsync(Guid id, bool tracking, CancellationToken ct);
    Task<bool> CustomerExistsForTafsiliAsync(string vahedCode, Guid tafsiliId, Guid? exceptId, CancellationToken ct);
    Task<bool> NationalIdExistsAsync(string vahedCode, string nationalId, Guid? exceptId, CancellationToken ct);
    Task AddCustomerAsync(SalesCustomer customer, CancellationToken ct);

    // ─── اسناد ───
    Task<(IReadOnlyList<SalesDoc> Items, int Total)> ListDocsAsync(SalesDocFilter filter, CancellationToken ct);
    /// <summary>سند با ردیف‌ها؛ <paramref name="tracking"/> برای ویرایش.</summary>
    Task<SalesDoc?> GetDocAsync(Guid id, bool tracking, CancellationToken ct);
    /// <summary>سندهای زندهٔ (نه باطل، نه حذف‌شده) صادرشده از این مبدأها، با ردیف‌ها.</summary>
    Task<IReadOnlyList<SalesDoc>> GetChildrenAsync(IReadOnlyCollection<Guid> sourceIds, CancellationToken ct);
    /// <summary>سفارش‌های تأییدشدهٔ مشتری با ردیف‌ها (برای «سفارش باز» در کنترل اعتبار).</summary>
    Task<IReadOnlyList<SalesDoc>> GetApprovedOrdersAsync(Guid customerId, CancellationToken ct);
    Task AddDocAsync(SalesDoc doc, CancellationToken ct);
    void AddLines(IEnumerable<SalesDocLine> lines);
    void RemoveLines(IEnumerable<SalesDocLine> lines);
    /// <summary>بزرگ‌ترین شمارهٔ ترتیبی داده‌شده برای (واحد، سال، نوع).</summary>
    Task<int> MaxDocSequenceAsync(string vahedCode, string year, SalesDocType type, CancellationToken ct);
    Task AddHistoryAsync(SalesDocHistory history, CancellationToken ct);
    Task<IReadOnlyList<SalesDocHistory>> ListHistoryAsync(Guid docId, CancellationToken ct);
}

/// <summary>مانده بدهکار مشتری روی حساب دریافتنی از سندهای حسابداری (حذف‌نشده، همهٔ وضعیت‌ها).</summary>
public interface ISalesLedgerReader
{
    /// <remarks>فقط سال مالی <paramref name="year"/> — سند افتتاحیهٔ هر سال مانده سال قبل را دوباره می‌آورد.</remarks>
    Task<long> ReceivableBalanceAsync(string vahedCode, string year, Guid accountId, Guid tafsiliId, CancellationToken ct);
}

public sealed record SalesCustomerRow(SalesCustomer Customer, string? TafsiliCode, string? TafsiliName);

public sealed record SalesDocFilter(
    string VahedCode, SalesDocType? Type, SalesDocStatus? Status, Guid? CustomerId, string? Search,
    string? FromDate, string? ToDate, int Skip, int Take);

public static class SalesLabels
{
    public static string Type(SalesDocType t) => t switch
    {
        SalesDocType.ProForma => "پیش‌فاکتور",
        SalesDocType.Order => "سفارش فروش",
        SalesDocType.Delivery => "حواله تحویل",
        SalesDocType.Invoice => "فاکتور فروش",
        SalesDocType.Return => "برگشت از فروش",
        _ => "سند فروش",
    };

    public static string Prefix(SalesDocType t) => t switch
    {
        SalesDocType.ProForma => "PF",
        SalesDocType.Order => "SO",
        SalesDocType.Delivery => "DN",
        SalesDocType.Invoice => "INV",
        _ => "RET",
    };

    /// <summary>حواله، فاکتور و برگشت قطعی می‌شوند و سند حسابداری دارند.</summary>
    public static bool IsPostable(SalesDocType t) => t is SalesDocType.Delivery or SalesDocType.Invoice or SalesDocType.Return;
}
