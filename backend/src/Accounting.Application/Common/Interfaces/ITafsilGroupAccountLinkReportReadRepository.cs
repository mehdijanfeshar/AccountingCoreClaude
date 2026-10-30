using Accounting.Application.Reports.TafsilGroupAccountLinks;

namespace Accounting.Application.Common.Interfaces;

/// <summary>سمت خواندن گزارش «ارتباط گروه تفصیلی با معین» — فراگیر و فیلترپذیر روی همهٔ معین‌ها،
/// برخلاف <c>GetAccountTafsilGroupLinksQuery</c> که فقط تک‌معینی است (فرم کدینگ حساب).</summary>
public interface ITafsilGroupAccountLinkReportReadRepository
{
    Task<IReadOnlyList<TafsilGroupAccountLinkReportRowDto>> GetAsync(
        TafsilGroupAccountLinkFilter filter, CancellationToken cancellationToken = default);
}
