using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Reports.AccountReview.GetReportProvinces;

/// <summary>یک استان در فهرست انتخاب دامنهٔ گزارش، با تعداد واحدهایش.</summary>
public sealed record ReportProvinceDto(string Code, string Name, int UnitCount);

/// <summary>
/// استان‌هایی که دست‌کم یک واحد دارند (از شهر واحدها) — برای انتخاب «استان» در دامنهٔ گزارش. فقط فهرست
/// است؛ اجازهٔ انتخاب استان را <c>ReportUnitScopeBehavior</c> کنترل می‌کند.
/// </summary>
public sealed record GetReportProvincesQuery : IRequest<IReadOnlyList<ReportProvinceDto>>;

public sealed class GetReportProvincesQueryHandler : IRequestHandler<GetReportProvincesQuery, IReadOnlyList<ReportProvinceDto>>
{
    private readonly IUnitAccessReadRepository _units;

    public GetReportProvincesQueryHandler(IUnitAccessReadRepository units) => _units = units;

    public async Task<IReadOnlyList<ReportProvinceDto>> Handle(GetReportProvincesQuery request, CancellationToken cancellationToken)
        => (await _units.GetUnitProvincesAsync(cancellationToken)).Values
            .GroupBy(p => p.ProvinceCode, StringComparer.Ordinal)
            .Select(g => new ReportProvinceDto(g.Key, g.First().ProvinceName, g.Count()))
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .ToList();
}
