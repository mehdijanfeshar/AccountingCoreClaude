using System.Text.Json.Serialization;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace Accounting.Application.Reports.AgePyramid;

// هرم سنی — طبق تعریف صاحب پروژه (۲۰۲۶-۱۰-۲۹)، نه گزارش موجودیِ یک‌سالهٔ مرجع: برای هر (معین شناسه‌دار،
// شناسه) سالِ شروع تعیین می‌شود، سنِ شناسه در سالِ گزارش محاسبه می‌شود، و اگر سن ≤۱۰ سال باشد (نه
// بیشتر — تصحیح صاحب پروژه ۲۰۲۶-۱۰-۲۹ روی تصمیم اولیهٔ «≥۱۰») ماندهٔ تجمعی تا پایانِ هر سال از شروع تا
// سالِ گزارش نشان داده می‌شود.
//
// تعیین سالِ شروع:
// • معین با FLAG=«تاریخ» (TB_ATTRIBFORACCOUNTCODE.FLAG=Date): از خودِ رشتهٔ شناسه. فرمت می‌تواند هم
//   ۴رقمی («۱۳۹۷…») و هم ۲رقمی («۹۷…») باشد (تصمیم صاحب پروژه: «هر دو حالت می‌تونه پیش بیاد»)؛
//   ابتدا ۴ رقم اول را سال معتبر می‌خوانیم، وگرنه ۲ رقم اول را با سده‌ای که به سال گزارش نزدیک‌تر و از
//   آن بزرگ‌تر نباشد («۱۴» قبل از «۱۳» — دوران جاری).
// • وگرنه (معین عددی، یا شناسهٔ تاریخی که از رشته قابل‌استخراج نبود): سالِ اولین سندی که این شناسه در
//   آن دیده شده (تصمیم صاحب پروژه: «از اولین سند در هر سالی که در نظر گرفته بشه»).

public sealed record AgePyramidYearBalanceDto(string Year, decimal Balance);

public sealed record AgePyramidRowDto(
    string AccCode, string? AccName, string Identifier, string StartYear, int Age, IReadOnlyList<AgePyramidYearBalanceDto> Yearly);

public sealed record GetAgePyramidReportQuery(
    string Year, IReadOnlyList<Guid>? AccountIds, string? FromIdentifier, string? ToIdentifier)
    : IRequest<IReadOnlyList<AgePyramidRowDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class GetAgePyramidReportQueryValidator : AbstractValidator<GetAgePyramidReportQuery>
{
    public GetAgePyramidReportQueryValidator()
    {
        RuleFor(x => x.Year).NotEmpty().Matches("^[0-9]{4}$").WithMessage("سال مالی باید چهار رقم باشد.");
        RuleFor(x => x.FromIdentifier).MaximumLength(10);
        RuleFor(x => x.ToIdentifier).MaximumLength(10);
    }
}

public sealed class GetAgePyramidReportQueryHandler(IAgePyramidReadRepository repository)
    : IRequestHandler<GetAgePyramidReportQuery, IReadOnlyList<AgePyramidRowDto>>
{
    private const int MaxAgeYears = 10;
    private const int MaxDisplayedYears = 10;

    public async Task<IReadOnlyList<AgePyramidRowDto>> Handle(GetAgePyramidReportQuery request, CancellationToken cancellationToken)
    {
        var reportYear = int.Parse(request.Year);
        var raw = await repository.GetAllYearsAsync(request.VahedCode, request.AccountIds, cancellationToken);

        var filtered = raw
            .Where(r => int.Parse(r.Year) <= reportYear)
            .Where(r => request.FromIdentifier is null || string.CompareOrdinal(r.Identifier, request.FromIdentifier) >= 0)
            .Where(r => request.ToIdentifier is null || string.CompareOrdinal(r.Identifier, request.ToIdentifier) <= 0);

        var rows = new List<AgePyramidRowDto>();
        foreach (var g in filtered.GroupBy(r => (r.AccountId, r.AccCode, r.AccName, r.Identifier, r.Flag)))
        {
            var byYear = g.GroupBy(r => int.Parse(r.Year)).ToDictionary(x => x.Key, x => x.Sum(r => r.Debtor - r.Creditor));
            var startYear = ResolveStartYear(g.Key.Flag, g.Key.Identifier, byYear.Keys, reportYear);
            var age = reportYear - startYear;
            if (age > MaxAgeYears)
                continue;

            // ماندهٔ تجمعی از سالِ شروعِ واقعی محاسبه می‌شود (درست بماند)، ولی فقط آخرین ۱۰ سال نمایش
            // داده می‌شود — تصمیم صاحب پروژه (۲۰۲۶-۱۰-۲۹): «۱۰ سال و کمتر، نه بیشتر» ستون‌های سال.
            var displayFromYear = Math.Max(startYear, reportYear - (MaxDisplayedYears - 1));
            decimal running = 0;
            var yearly = new List<AgePyramidYearBalanceDto>();
            for (var y = startYear; y <= reportYear; y++)
            {
                running += byYear.GetValueOrDefault(y);
                if (y >= displayFromYear)
                    yearly.Add(new AgePyramidYearBalanceDto(y.ToString(), running));
            }

            rows.Add(new AgePyramidRowDto(g.Key.AccCode, g.Key.AccName, g.Key.Identifier, startYear.ToString(), age, yearly));
        }

        return rows.OrderBy(r => r.AccCode, StringComparer.Ordinal).ThenBy(r => r.Identifier, StringComparer.Ordinal).ToList();
    }

    /// <summary>سالِ شروعِ یک شناسه؛ هیچ‌وقت بزرگ‌تر از <paramref name="reportYear"/> برنمی‌گردد
    /// (نبودِ داده پیش از سالِ گزارش معنا ندارد، پس حداقل اولین سالِ دیده‌شده تضمین‌شده است).</summary>
    private static int ResolveStartYear(AttribFlag? flag, string identifier, IReadOnlyCollection<int> seenYears, int reportYear)
    {
        var fromIdentifier = flag == AttribFlag.Date ? ParseYearPrefix(identifier, reportYear) : null;
        var firstSeen = seenYears.Min();
        return fromIdentifier is { } y && y <= firstSeen ? y : firstSeen;
    }

    private static int? ParseYearPrefix(string identifier, int reportYear)
    {
        if (identifier.Length >= 4 && int.TryParse(identifier[..4], out var y4) && y4 is >= 1300 and <= 1500 && y4 <= reportYear)
            return y4;
        if (identifier.Length >= 2 && int.TryParse(identifier[..2], out var y2) && y2 is >= 0 and <= 99)
        {
            var c14 = 1400 + y2;
            if (c14 <= reportYear)
                return c14;
            var c13 = 1300 + y2;
            if (c13 <= reportYear)
                return c13;
        }
        return null;
    }
}
