using System.Text.Json.Serialization;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.Vouchers.YearEnd;
using Accounting.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace Accounting.Application.Reports.OpeningBalanceDiscrepancy;

// مغایرت افتتاحیه با ماندهٔ سال قبل — معادل گزارش «مغایرت افتتاحیه با مانده سال قبل» منوی قدیم. مرجع
// این مقایسه را روی دو View تأییدنشده (vwclosingbalance/vwFirstBalance) می‌ساخت که در این پروژه موجود
// نیستند و با ریسک Viewهای غلط دیگر (#۲۶/#۳۰/#۳۴) تکرار نمی‌شوند؛ به‌جایش طبق تصمیم صاحب پروژه
// (۲۰۲۶-۱۰-۲۹) مستقیم از رکوردها محاسبه می‌شود: برای هر ردیف واقعی سند افتتاحیهٔ سال جاری (به تفکیک
// معین، و اگر معین شناسه‌دار است به تفکیک شناسه)، مانده‌ای که همان معین/شناسه در کل سال قبل داشته
// محاسبه و با مبلغ افتتاحیه مقایسه می‌شود؛ نابرابری = مغایرت.
//
// عمداً انجام‌نشده: جهت برعکس (مانده‌ای در سال قبل که اصلاً در افتتاحیهٔ سال جاری ردیفی ندارد) — دقیقاً
// طبق توضیح صاحب پروژه، مسیر فقط از سمت سند افتتاحیه است.

public sealed record OpeningBalanceAccountLine(Guid AccountId, string AccCode, string? AccName, string? AttributeValue, decimal Net);

public sealed record OpeningBalanceDiscrepancyRowDto(
    string AccCode, string? AccName, string? Identifier, decimal OpeningAmount, decimal PriorYearBalance, decimal Difference);

public sealed record GetOpeningBalanceDiscrepancyQuery(string Year)
    : IRequest<IReadOnlyList<OpeningBalanceDiscrepancyRowDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class GetOpeningBalanceDiscrepancyQueryValidator : AbstractValidator<GetOpeningBalanceDiscrepancyQuery>
{
    public GetOpeningBalanceDiscrepancyQueryValidator()
        => RuleFor(x => x.Year).NotEmpty().Matches("^[0-9]{4}$").WithMessage("سال مالی باید چهار رقم باشد.");
}

public sealed class GetOpeningBalanceDiscrepancyQueryHandler(
    IYearEndRepository yearEndRepository, IOpeningBalanceDiscrepancyReadRepository repository)
    : IRequestHandler<GetOpeningBalanceDiscrepancyQuery, IReadOnlyList<OpeningBalanceDiscrepancyRowDto>>
{
    public async Task<IReadOnlyList<OpeningBalanceDiscrepancyRowDto>> Handle(
        GetOpeningBalanceDiscrepancyQuery request, CancellationToken cancellationToken)
    {
        var iface = await yearEndRepository.GetInterfaceAccountAsync(InterfaceType.OpenVoucher, cancellationToken)
            ?? throw new BusinessRuleException("حساب رابط افتتاحیه تعریف نشده است (اطلاعات پایه، انتساب رابط، نوع «افتتاحیه»).");

        var priorYear = (int.Parse(request.Year) - 1).ToString("0000");
        var opening = await repository.GetOpeningLinesAsync(request.VahedCode, request.Year, iface.Id, cancellationToken);
        var prior = (await repository.GetYearBalancesByAttributeAsync(request.VahedCode, priorYear, cancellationToken))
            .ToDictionary(b => (b.AccountId, b.AttributeValue), b => b.Net);

        return opening
            .Select(o => new OpeningBalanceDiscrepancyRowDto(
                o.AccCode, o.AccName, o.AttributeValue, o.Net,
                prior.GetValueOrDefault((o.AccountId, o.AttributeValue)),
                o.Net - prior.GetValueOrDefault((o.AccountId, o.AttributeValue))))
            .Where(r => r.Difference != 0)
            .OrderBy(r => r.AccCode, StringComparer.Ordinal).ThenBy(r => r.Identifier, StringComparer.Ordinal)
            .ToList();
    }
}
