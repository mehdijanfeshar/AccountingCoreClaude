using Accounting.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace Accounting.Application.Reports.TafsilGroupAccountLinks;

// گزارش «ارتباط گروه تفصیلی با معین» — معادل گزارش «ارتباط» منوی قدیم که روی TB_ACCOUNT_LINK_TAFSIL_GROUP
// (مرجع) / TB_ACCOUNT_LINK_TAFSILGROUP (اینجا) ساخته می‌شد. CRUD تک‌معینی همین جدول در فاز ۲۳ وجود دارد
// (GetAccountTafsilGroupLinksQuery، پشت فرم کدینگ حساب)؛ این یکی فراگیر و فیلترپذیر روی همهٔ معین‌هاست —
// دقیقاً مثل مرجع که با فیلترهای معین (چندتایی)، سطح، گروه تفصیلی و تفصیلی جست‌وجو می‌شد.
// جدول VAHEDCODE ندارد (همان استدلال GetAccountTafsilGroupLinksQuery) پس IVahedScopedQuery نیست.

/// <param name="AccountIds">فیلتر معین (چندتایی)؛ خالی/null = همهٔ معین‌ها.</param>
/// <param name="LevelId">فیلتر سطح تفصیلی.</param>
/// <param name="TafsilGroupId">فیلتر گروه تفصیلی.</param>
/// <param name="TafsiliId">فیلتر تفصیلی — فقط گروه‌هایی که این تفصیلی به آن‌ها وصل است (TB_TAFSIL_LINK_TAFSILGROUP).</param>
public sealed record TafsilGroupAccountLinkFilter(
    IReadOnlyList<Guid>? AccountIds, Guid? LevelId, Guid? TafsilGroupId, Guid? TafsiliId);

public sealed record TafsilGroupAccountLinkReportRowDto(
    Guid AccountId, string MoeinCode, string? MoeinTitle,
    Guid LevelId, string LevelTitle,
    Guid TafsilGroupId, string GroupCode, string GroupTitle);

public sealed record GetTafsilGroupAccountLinkReportQuery(
    IReadOnlyList<Guid>? AccountIds, Guid? LevelId, Guid? TafsilGroupId, Guid? TafsiliId)
    : IRequest<IReadOnlyList<TafsilGroupAccountLinkReportRowDto>>;

public sealed class GetTafsilGroupAccountLinkReportQueryValidator : AbstractValidator<GetTafsilGroupAccountLinkReportQuery>
{
    public GetTafsilGroupAccountLinkReportQueryValidator()
        => RuleFor(x => x.AccountIds!.Count).LessThanOrEqualTo(200).WithMessage("حداکثر ۲۰۰ معین در هر بار.")
            .When(x => x.AccountIds is not null);
}

public sealed class GetTafsilGroupAccountLinkReportQueryHandler(ITafsilGroupAccountLinkReportReadRepository repository)
    : IRequestHandler<GetTafsilGroupAccountLinkReportQuery, IReadOnlyList<TafsilGroupAccountLinkReportRowDto>>
{
    public Task<IReadOnlyList<TafsilGroupAccountLinkReportRowDto>> Handle(
        GetTafsilGroupAccountLinkReportQuery request, CancellationToken cancellationToken)
        => repository.GetAsync(
            new TafsilGroupAccountLinkFilter(request.AccountIds, request.LevelId, request.TafsilGroupId, request.TafsiliId),
            cancellationToken);
}
