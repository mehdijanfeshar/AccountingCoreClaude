using System.Text.Json.Serialization;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace Accounting.Application.Reports.GeneralLedger;

/// <summary>
/// دفتر کل — برای هر حساب کل: ماندهٔ ابتدای دوره، یک سطر به‌ازای هر سند (تاریخ، شماره، شرح، بدهکار، بستانکار،
/// ماندهٔ جاری) و جمع دوره. معادل «دفتر کل» سیستم قدیم (<c>Report/LedgerReport</c>)، ولی از جدول‌ها:
/// <c>VW_LEDGERREPORT</c> مرجع حذف‌شده‌ها را کنار نمی‌گذارد، روی DOCLIFE گروه می‌کند (یک کل در چند سطر) و
/// تاریخ/شمارهٔ سند ندارد — استثنای قاعدهٔ ۹ (ریسک #۳۴). «ابتدای دوره» همان تعریف تراز آزمایشی است:
/// اسناد همان سال با تاریخ پیش از «از تاریخ».
/// </summary>
public sealed record GetGeneralLedgerQuery(
    string Year,
    string? FromDate = null,
    string? ToDate = null,
    string? FromKol = null,
    string? ToKol = null,
    int? DocLife = null,
    string? FromVoucherNo = null,
    string? ToVoucherNo = null,
    int PageNumber = 1,
    int PageSize = 100) : IRequest<GeneralLedgerResultDto>, IMultiUnitReportQuery
{
    public ReportUnitScopeMode UnitScope { get; init; }

    public UnitCategory? UnitCategory { get; init; }

    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary>جمع هر حساب کل. مبالغ با علامت حسابداری (بدهکار مثبت).</summary>
public sealed record GeneralLedgerAccountDto(
    string KolCode,
    string? KolName,
    decimal Opening,
    decimal PeriodDebit,
    decimal PeriodCredit,
    decimal Closing);

/// <summary>یک سند روی یک حساب کل. <see cref="Balance"/> = ماندهٔ جاری پس از این سند (بدهکار مثبت).</summary>
public sealed record GeneralLedgerRowDto(
    string KolCode,
    Guid VoucherHeadId,
    string? VahedCode,
    string? DateDoc,
    string? DocNum,
    string? Description,
    decimal Debit,
    decimal Credit,
    decimal Balance);

public sealed record GeneralLedgerResultDto(
    IReadOnlyList<GeneralLedgerAccountDto> Accounts,
    IReadOnlyList<GeneralLedgerRowDto> Rows,
    int PageNumber,
    int PageSize,
    int TotalCount);

public interface IGeneralLedgerReadRepository
{
    Task<GeneralLedgerResultDto> GetAsync(GetGeneralLedgerQuery query, CancellationToken cancellationToken = default);
}

public sealed class GetGeneralLedgerQueryHandler : IRequestHandler<GetGeneralLedgerQuery, GeneralLedgerResultDto>
{
    private readonly IGeneralLedgerReadRepository _repository;

    public GetGeneralLedgerQueryHandler(IGeneralLedgerReadRepository repository)
    {
        _repository = repository;
    }

    public Task<GeneralLedgerResultDto> Handle(GetGeneralLedgerQuery request, CancellationToken cancellationToken)
        => _repository.GetAsync(request, cancellationToken);
}

public sealed class GetGeneralLedgerQueryValidator : AbstractValidator<GetGeneralLedgerQuery>
{
    public GetGeneralLedgerQueryValidator()
    {
        RuleFor(x => x.Year).Matches("^[0-9]{4}$").WithMessage("سال مالی باید ۴ رقم باشد.");
        RuleFor(x => x.FromDate).Matches("^[0-9]{8}$").WithMessage("تاریخ باید به شکل YYYYMMDD باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.FromDate));
        RuleFor(x => x.ToDate).Matches("^[0-9]{8}$").WithMessage("تاریخ باید به شکل YYYYMMDD باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.ToDate));
        RuleFor(x => x).Must(x => string.CompareOrdinal(x.FromDate, x.ToDate) <= 0)
            .WithMessage("«از تاریخ» نباید بعد از «تا تاریخ» باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.FromDate) && !string.IsNullOrWhiteSpace(x.ToDate));
        RuleFor(x => x.FromKol).Matches("^[0-9]{1,4}$").WithMessage("کد کل حداکثر ۴ رقم است.")
            .When(x => !string.IsNullOrWhiteSpace(x.FromKol));
        RuleFor(x => x.ToKol).Matches("^[0-9]{1,4}$").WithMessage("کد کل حداکثر ۴ رقم است.")
            .When(x => !string.IsNullOrWhiteSpace(x.ToKol));
        RuleFor(x => x.FromVoucherNo).Matches("^[0-9]{1,6}$").WithMessage("شمارهٔ سند حداکثر ۶ رقم است.")
            .When(x => !string.IsNullOrWhiteSpace(x.FromVoucherNo));
        RuleFor(x => x.ToVoucherNo).Matches("^[0-9]{1,6}$").WithMessage("شمارهٔ سند حداکثر ۶ رقم است.")
            .When(x => !string.IsNullOrWhiteSpace(x.ToVoucherNo));
        RuleFor(x => x.DocLife).InclusiveBetween(1, 4).When(x => x.DocLife.HasValue);
        RuleFor(x => x.PageNumber).InclusiveBetween(1, 1_000_000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 1000);
    }
}
