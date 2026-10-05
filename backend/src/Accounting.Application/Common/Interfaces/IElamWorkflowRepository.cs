using Accounting.Application.Common;
using Accounting.Application.Elams;
using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// سمت نوشتن گردش اعلامیه (سرسند + ردیف + تفصیلی + شناسهٔ ردیف سند). همه‌چیز فقط stage می‌شود؛
/// ذخیره با <see cref="IUnitOfWork"/> در Handler.
/// </summary>
public interface IElamWorkflowRepository
{
    /// <summary>سرسند با ردیف‌ها و تفصیلی‌های فعال، tracked. ردیف واحد دیگر ⇒ ۴۰۳، نبود ⇒ null.</summary>
    Task<TB_ELAMHEAD?> GetWithDetailsForUpdateAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>سریال بعدی ۱۴ رقمی: واحد(۴) + سال(۴) + شمارهٔ ۶ رقمی، به‌ازای (واحد، سال، کد رابط).</summary>
    Task<string> GetNextSerialAsync(string vahedCode, string year, string elamCode, CancellationToken cancellationToken = default);

    /// <summary>حساب رابط برای کد نوع رابط («1» صادره، «2» رسیده، «3» درآمد)؛ null = تعریف نشده.</summary>
    Task<ElamRabetAccount?> GetRabetAccountAsync(string rabetTypeCode, CancellationToken cancellationToken = default);

    Task<string?> GetVahedNameAsync(string vahedCode, CancellationToken cancellationToken = default);

    Task AddHeadAsync(TB_ELAMHEAD head, CancellationToken cancellationToken = default);

    Task AddDetailAsync(TB_ELAMDETAIL detail, CancellationToken cancellationToken = default);

    Task AddDetailLinkAsync(TB_ELAMDETAIL_LINK_TAFSILI link, CancellationToken cancellationToken = default);

    /// <summary>شناسهٔ تعریف «حساب شناسه‌دار» معین در واحد و سال؛ null = معین شناسه ندارد.</summary>
    Task<Guid?> GetAttribDefinitionIdAsync(Guid accountId, string vahedCode, string year, CancellationToken cancellationToken = default);

    Task AddAttribInVoucherAsync(TB_ATTRIBSINVOUCHER row, CancellationToken cancellationToken = default);
}

/// <summary>سمت خواندن اعلامیه.</summary>
public interface IElamWorkflowReadRepository
{
    Task<PagedResult<ElamCartableItemDto>> GetCartableAsync(ElamCartableFilter filter, CancellationToken cancellationToken = default);

    Task<ElamViewDto?> GetAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>همهٔ واحدها به‌جز واحد جاری — مقصد اعلامیه می‌تواند هر واحدی باشد.</summary>
    Task<IReadOnlyList<ElamUnitDto>> GetUnitsAsync(string excludeVahedCode, CancellationToken cancellationToken = default);
}
