using System.Text.Json.Serialization;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.Commands.Vouchers.Common;
using FluentValidation;
using MediatR;

namespace Accounting.Application.Queries.Accounts.GetEntryMoins;

/// <summary>
/// لیست انتخاب معینِ ثبت سند: فقط حساب‌های <b>معین</b> (نه گروه و کل) که آرتیکل دستی روی آن‌ها برای
/// نوع واحد کاربر در تاریخ سند مجاز است — همان قاعدهٔ <see cref="IAccountEntryPolicy"/> که ذخیرهٔ سند را
/// کنترل می‌کند، تا کاربر معینی نبیند که بعداً ذخیره‌اش رد شود. واحد فقط از هدر.
/// </summary>
/// <param name="DateDoc">تاریخ سند <c>yyyyMMdd</c>؛ خالی = فقط ردیف‌های بی‌بازه.</param>
public sealed record GetEntryMoinsQuery(string? Search, string? DateDoc, int PageNumber, int PageSize)
    : IRequest<PagedResult<AccountCodeDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class GetEntryMoinsQueryValidator : AbstractValidator<GetEntryMoinsQuery>
{
    public GetEntryMoinsQueryValidator()
    {
        RuleFor(x => x.PageNumber).InclusiveBetween(1, 100_000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.DateDoc).MaximumLength(10);
    }
}

public sealed class GetEntryMoinsQueryHandler : IRequestHandler<GetEntryMoinsQuery, PagedResult<AccountCodeDto>>
{
    private readonly IAccountCodeReadRepository _accounts;
    private readonly IAccountEntryPolicy _policy;

    public GetEntryMoinsQueryHandler(IAccountCodeReadRepository accounts, IAccountEntryPolicy policy)
    {
        _accounts = accounts;
        _policy = policy;
    }

    public async Task<PagedResult<AccountCodeDto>> Handle(GetEntryMoinsQuery request, CancellationToken cancellationToken)
    {
        var date = request.DateDoc?.Replace("/", "").Replace("-", "").Trim();
        var allowed = await _policy.GetManualEntryAllowedIdsAsync(request.VahedCode, date, cancellationToken);

        // فیلتر و صفحه‌بندی در حافظه: فهرست مجاز می‌تواند بیش از ۱۰۰۰ شناسه باشد (سقف IN اوراکل)،
        // و تعداد معین‌ها برای بارگذاری یک‌جا کوچک است.
        var moins = (await _accounts.ListMoinsAsync(request.Search, cancellationToken))
            .Where(a => allowed.Contains(a.Id))
            .ToList();

        return new PagedResult<AccountCodeDto>
        {
            Items = moins.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToList(),
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = moins.Count,
        };
    }
}
