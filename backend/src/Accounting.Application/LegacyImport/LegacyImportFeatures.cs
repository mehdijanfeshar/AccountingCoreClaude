using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using FluentValidation;
using MediatR;

namespace Accounting.Application.LegacyImport;

/// <summary>
/// انتقال کدینگ یک واحد از سیستم قدیم: یافتن اسکیماهای سالانه، دریافت کاربرگ (کاربر کد ملی/شناسهٔ ملی اشخاص را
/// وارد می‌کند) و بارگذاری آن برای تعریف تفصیلی‌ها. واحد از هدر <c>X-Vahed-Code</c> (VahedScopeBehavior) می‌آید.
/// کدینگ تا سطح معین مشترک است و جدا، یک‌بار، در <c>LegacyChart</c> منتقل می‌شود.
/// </summary>
public sealed record GetLegacySourcesQuery : IRequest<IReadOnlyList<LegacySourceDto>>, IVahedScopedQuery
{
    public string VahedCode { get; set; } = string.Empty;
}

public sealed record GetLegacyConnectionsQuery : IRequest<LegacyConnectionsDto>, IVahedScopedQuery
{
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary>
/// اتصال واحد (سرور + سال‌ها) را می‌آزماید و فقط اگر همهٔ سال‌ها وصل شوند ذخیره می‌کند؛
/// <c>CheckOnly</c> = فقط آزمودن (دکمهٔ «بررسی اتصال»).
/// </summary>
public sealed record SaveLegacyConnectionsCommand(LegacyConnectionsInput Input, bool CheckOnly) : IRequest<LegacyConnectionsResultDto>, IVahedScopedCommand
{
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary>رمزگشایی نام کاربر/رمز سرور انتخاب‌شده از فایل XML سیستم قدیم.</summary>
public sealed record ReadLegacyXmlServerCommand(LegacyXmlServerInput Input) : IRequest<LegacyXmlServerDto>;

internal static class LegacyRules
{
    public const string OracleName = "^[A-Za-z][A-Za-z0-9_$#]{0,127}$";
}

public sealed class SaveLegacyConnectionsCommandValidator : AbstractValidator<SaveLegacyConnectionsCommand>
{
    public SaveLegacyConnectionsCommandValidator()
    {
        RuleFor(x => x.Input).NotNull();
        RuleFor(x => x.Input.Server).NotNull().WithMessage("سرور را تعیین کنید.");
        RuleFor(x => x.Input.Server.DataSource).NotEmpty().WithMessage("آدرس سرور را وارد کنید.").MaximumLength(1000)
            .Must(s => s is null || !s.Contains(';')).WithMessage("آدرس سرور نباید «;» داشته باشد.")
            .When(x => x.Input?.Server != null);
        RuleFor(x => x.Input.Server.SchemaBase).Matches(LegacyRules.OracleName).WithMessage("نام اسکیما معتبر نیست.")
            .When(x => !string.IsNullOrEmpty(x.Input?.Server?.SchemaBase));
        RuleFor(x => x.Input.Server.UserName).Matches(LegacyRules.OracleName).WithMessage("نام کاربر اوراکل معتبر نیست.")
            .When(x => !string.IsNullOrEmpty(x.Input?.Server?.UserName));
        RuleFor(x => x.Input.Server.Password).MaximumLength(128).When(x => x.Input?.Server != null);
        // فهرست خالی سال‌ها در «بررسی» = یافتن خودکار سال‌های موجود روی سرور
        RuleFor(x => x.Input.Years).NotEmpty().WithMessage("دست‌کم یک سال مالی وارد کنید.")
            .When(x => x.Input != null && !x.CheckOnly);
        RuleFor(x => x.Input.Years).NotNull()
            .Must(y => y.Count <= 50).WithMessage("حداکثر ۵۰ سال.")
            .Must(y => y.Select(x => x.Year).Distinct().Count() == y.Count).WithMessage("هر سال مالی فقط یک بار.")
            .When(x => x.Input != null);
        RuleForEach(x => x.Input.Years).ChildRules(y =>
        {
            y.RuleFor(x => x.Year).InclusiveBetween(1300, 1499).WithMessage("سال مالی باید ۴ رقمی (مثلاً ۱۴۰۰) باشد.");
            y.RuleFor(x => x.Schema).NotEmpty().Matches(LegacyRules.OracleName).WithMessage("نام اسکیما معتبر نیست.");
            y.RuleFor(x => x.UserName).Matches(LegacyRules.OracleName).WithMessage("نام کاربر اوراکل معتبر نیست.")
                .When(x => !string.IsNullOrEmpty(x.UserName));
            y.RuleFor(x => x.Password).MaximumLength(128);
        }).When(x => x.Input?.Years != null);
    }
}

public sealed class ReadLegacyXmlServerCommandValidator : AbstractValidator<ReadLegacyXmlServerCommand>
{
    public ReadLegacyXmlServerCommandValidator()
    {
        RuleFor(x => x.Input.DataSource).NotEmpty().MaximumLength(1000).Must(s => s is null || !s.Contains(';'));
        RuleFor(x => x.Input.Title).MaximumLength(200);
        RuleFor(x => x.Input.EncryptedUserName).MaximumLength(4000);
        RuleFor(x => x.Input.EncryptedPassword).MaximumLength(4000);
    }
}

public sealed record GetLegacyImportStatusQuery : IRequest<LegacyImportStatusDto>, IVahedScopedQuery
{
    public string VahedCode { get; set; } = string.Empty;
}

/// <param name="Year">فقط این سال (خالی = همهٔ سال‌های ذخیره‌شده)؛ کاربرگ یک سال فقط برای مشاهده است.</param>
public sealed record GetLegacyUnitWorkbookQuery(int? Year = null) : IRequest<LegacyFileDto>, IVahedScopedQuery
{
    public string VahedCode { get; set; } = string.Empty;
}

/// <param name="Workbook">کاربرگ پرشدهٔ واحد (xlsx).</param>
/// <param name="Commit">false = فقط بررسی (همه‌چیز در تراکنش اجرا و برگردانده می‌شود).</param>
public sealed record ImportLegacyUnitCodingCommand(byte[] Workbook, bool Commit) : IRequest<LegacyImportReportDto>, IVahedScopedCommand
{
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class ImportLegacyUnitCodingCommandValidator : AbstractValidator<ImportLegacyUnitCodingCommand>
{
    public const int MaxBytes = 20 * 1024 * 1024;

    public ImportLegacyUnitCodingCommandValidator()
    {
        RuleFor(x => x.Workbook).NotEmpty().WithMessage("فایل کاربرگ خالی است.")
            .Must(b => b.Length <= MaxBytes).WithMessage("حجم فایل کاربرگ بیش از حد مجاز است.");
    }
}

/// <param name="Year">سال مالی هدف (اسناد با تاریخ همین سال، از همهٔ اسکیماها).</param>
/// <param name="Commit">false = فقط بررسی (همه‌چیز در تراکنش اجرا و برگردانده می‌شود).</param>
public sealed record ImportLegacyVouchersCommand(int Year, bool Commit) : IRequest<LegacyImportReportDto>, IVahedScopedCommand
{
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class ImportLegacyVouchersCommandValidator : AbstractValidator<ImportLegacyVouchersCommand>
{
    public ImportLegacyVouchersCommandValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(1300, 1499).WithMessage("سال مالی باید ۴ رقمی (مثلاً ۱۴۰۰) باشد.");
    }
}

/// <param name="Commit">false = فقط بررسی.</param>
public sealed record ImportLegacyBankCommand(bool Commit) : IRequest<LegacyImportReportDto>, IVahedScopedCommand
{
    public string VahedCode { get; set; } = string.Empty;
}

public sealed record GetLegacyVoucherStatusQuery : IRequest<IReadOnlyList<LegacyVoucherYearDto>>, IVahedScopedQuery
{
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary>فهرست شمارهٔ جدید ← قدیم اسناد منتقل‌شدهٔ یک سال (xlsx).</summary>
public sealed record GetLegacyVoucherMapQuery(int Year) : IRequest<LegacyFileDto>, IVahedScopedQuery
{
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class LegacyImportHandlers :
    IRequestHandler<ImportLegacyVouchersCommand, LegacyImportReportDto>,
    IRequestHandler<ImportLegacyBankCommand, LegacyImportReportDto>,
    IRequestHandler<GetLegacyVoucherStatusQuery, IReadOnlyList<LegacyVoucherYearDto>>,
    IRequestHandler<GetLegacyVoucherMapQuery, LegacyFileDto>,
    IRequestHandler<GetLegacyConnectionsQuery, LegacyConnectionsDto>,
    IRequestHandler<SaveLegacyConnectionsCommand, LegacyConnectionsResultDto>,
    IRequestHandler<ReadLegacyXmlServerCommand, LegacyXmlServerDto>,
    IRequestHandler<GetLegacySourcesQuery, IReadOnlyList<LegacySourceDto>>,
    IRequestHandler<GetLegacyImportStatusQuery, LegacyImportStatusDto>,
    IRequestHandler<GetLegacyUnitWorkbookQuery, LegacyFileDto>,
    IRequestHandler<ImportLegacyUnitCodingCommand, LegacyImportReportDto>
{
    private readonly ILegacyImportService _service;
    private readonly ICurrentUser _currentUser;

    public LegacyImportHandlers(ILegacyImportService service, ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    public Task<LegacyConnectionsDto> Handle(GetLegacyConnectionsQuery request, CancellationToken cancellationToken)
        => _service.GetConnectionsAsync(request.VahedCode, cancellationToken);

    public Task<LegacyConnectionsResultDto> Handle(SaveLegacyConnectionsCommand request, CancellationToken cancellationToken)
        => _service.SaveConnectionsAsync(request.VahedCode, request.Input, request.CheckOnly, cancellationToken);

    public Task<LegacyXmlServerDto> Handle(ReadLegacyXmlServerCommand request, CancellationToken cancellationToken)
        => Task.FromResult(_service.ReadXmlServer(request.Input));

    public Task<IReadOnlyList<LegacySourceDto>> Handle(GetLegacySourcesQuery request, CancellationToken cancellationToken)
        => _service.FindSourcesAsync(request.VahedCode, cancellationToken);

    public Task<LegacyImportStatusDto> Handle(GetLegacyImportStatusQuery request, CancellationToken cancellationToken)
        => _service.GetStatusAsync(request.VahedCode, cancellationToken);

    public Task<LegacyFileDto> Handle(GetLegacyUnitWorkbookQuery request, CancellationToken cancellationToken)
        => _service.BuildUnitWorkbookAsync(request.VahedCode, request.Year, cancellationToken);

    public Task<LegacyImportReportDto> Handle(ImportLegacyUnitCodingCommand request, CancellationToken cancellationToken)
        => _service.ImportUnitCodingAsync(request.VahedCode, request.Workbook, request.Commit, _currentUser.UserId, cancellationToken);

    public Task<LegacyImportReportDto> Handle(ImportLegacyBankCommand request, CancellationToken cancellationToken)
        => _service.ImportBankAsync(request.VahedCode, request.Commit, _currentUser.UserId, cancellationToken);

    public Task<LegacyImportReportDto> Handle(ImportLegacyVouchersCommand request, CancellationToken cancellationToken)
        => _service.ImportVouchersAsync(request.VahedCode, request.Year, request.Commit, _currentUser.UserId, cancellationToken);

    public Task<IReadOnlyList<LegacyVoucherYearDto>> Handle(GetLegacyVoucherStatusQuery request, CancellationToken cancellationToken)
        => _service.GetVoucherStatusAsync(request.VahedCode, cancellationToken);

    public Task<LegacyFileDto> Handle(GetLegacyVoucherMapQuery request, CancellationToken cancellationToken)
        => _service.BuildVoucherMapAsync(request.VahedCode, request.Year, cancellationToken);
}
