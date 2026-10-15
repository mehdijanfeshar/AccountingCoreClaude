using Accounting.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace Accounting.Application.LegacyChart;

/// <summary>
/// کدینگ مشترک گروه/کل/معین از سیستم قدیم — یک‌بار (با اولین واحد) و فقط مدیر ستاد
/// (ماژول در <c>AppRoles.HeadquartersOnlyModules</c>). کد واحد ندارد.
/// </summary>
/// <param name="VahedCodes">واحدهایی که کل/معینشان در کاربرگ جمع می‌شود.</param>
public sealed record GetLegacyChartWorkbookQuery(IReadOnlyList<string> VahedCodes) : IRequest<LegacyFileDto>;

public sealed class GetLegacyChartWorkbookQueryValidator : AbstractValidator<GetLegacyChartWorkbookQuery>
{
    public GetLegacyChartWorkbookQueryValidator()
    {
        RuleFor(x => x.VahedCodes).NotEmpty().WithMessage("دست‌کم یک واحد بدهید.");
        RuleForEach(x => x.VahedCodes).Matches("^[0-9]{4}$").WithMessage("کد واحد باید ۴ رقم باشد.");
    }
}

public sealed record ImportLegacyChartCommand(byte[] Workbook, bool Commit) : IRequest<LegacyImportReportDto>;

public sealed class ImportLegacyChartCommandValidator : AbstractValidator<ImportLegacyChartCommand>
{
    public ImportLegacyChartCommandValidator()
    {
        RuleFor(x => x.Workbook).NotEmpty().WithMessage("فایل کاربرگ خالی است.")
            .Must(b => b.Length <= 20 * 1024 * 1024).WithMessage("حجم فایل کاربرگ بیش از حد مجاز است.");
    }
}

public sealed class LegacyChartHandlers :
    IRequestHandler<GetLegacyChartWorkbookQuery, LegacyFileDto>,
    IRequestHandler<ImportLegacyChartCommand, LegacyImportReportDto>
{
    private readonly ILegacyImportService _service;
    private readonly ICurrentUser _currentUser;

    public LegacyChartHandlers(ILegacyImportService service, ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    public Task<LegacyFileDto> Handle(GetLegacyChartWorkbookQuery request, CancellationToken cancellationToken)
        => _service.BuildChartWorkbookAsync(request.VahedCodes, cancellationToken);

    public Task<LegacyImportReportDto> Handle(ImportLegacyChartCommand request, CancellationToken cancellationToken)
        => _service.ImportChartAsync(request.Workbook, request.Commit, _currentUser.UserId, cancellationToken);
}
