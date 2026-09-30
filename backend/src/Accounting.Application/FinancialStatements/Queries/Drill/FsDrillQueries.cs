using System.Text.Json.Serialization;
using FluentValidation;
using Accounting.Application.Common.Security;
using Accounting.Application.FinancialStatements.Engine;
using MediatR;

namespace Accounting.Application.FinancialStatements.Queries.Drill;

/// <summary>
/// <c>GET api/fs/runs/{runId}/rows/{rowId}/accounts</c> — سطح اول Drill-down (بخش ۴۵-د): سهم هر معین در
/// ردیف «حساب»، از Snapshot. اجرا/ردیفِ واحد دیگر یا ناموجود = <see langword="null"/> (۴۰۴).
/// </summary>
public sealed record GetFsRunRowAccountsQuery(Guid RunId, Guid RowId) : IRequest<IReadOnlyList<FsDrillAccountDto>?>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary>
/// <c>GET api/fs/runs/{runId}/rows/{rowId}/units?acc=</c> — سطح «واحد»: سهم هر زیرواحد سطح اول (در اجرای
/// ترکیبی) در ردیف، یا فقط در معین <paramref name="AccCode"/>؛ از Snapshot.
/// </summary>
public sealed record GetFsRunRowUnitsQuery(Guid RunId, Guid RowId, string? AccCode) : IRequest<IReadOnlyList<FsDrillUnitDto>?>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary>
/// <c>GET api/fs/runs/{runId}/rows/{rowId}/vouchers?acc=&amp;unit=&amp;column=&amp;page=&amp;pageSize=</c> — سطح
/// «سند»: ردیف‌های سند معین <paramref name="AccCode"/> که در مبلغ ردیف سهم دارند، <b>زنده</b> از اسناد با همان
/// فیلترهای اجرا (سال، دوره، حداقل وضعیت، بدون اختتامیه) و همان بخش دوره که <c>VALUE_TYPE</c> ردیف می‌خواند.
/// <paramref name="Unit"/> = زیرواحد سطح اول (و زیرمجموعه‌اش)؛ خالی = همهٔ واحدهای اجرا.
/// <paramref name="Column"/> = <c>CUR</c> یا <c>PRV</c> (سال قبل).
/// </summary>
public sealed record GetFsRunRowVouchersQuery(
    Guid RunId,
    Guid RowId,
    string AccCode,
    string? Unit,
    string Column,
    int PageNumber,
    int PageSize) : IRequest<FsDrillVoucherPageDto?>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary><c>GET api/fs/runs/{runId}/excel</c> — فایل Excel اجرا (هر صورت و یادداشت یک برگه، جمع‌ها فرمول).</summary>
public sealed record GetFsRunExcelQuery(Guid RunId) : IRequest<FsFileDto?>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class GetFsRunRowVouchersQueryValidator : AbstractValidator<GetFsRunRowVouchersQuery>
{
    public GetFsRunRowVouchersQueryValidator()
    {
        RuleFor(x => x.AccCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Unit).MaximumLength(4);
        RuleFor(x => x.Column).Must(c => c is FsColumns.Current or FsColumns.Prior).WithMessage("ستون باید CUR یا PRV باشد.");
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
