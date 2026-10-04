using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace Accounting.Application.FinancialStatements.Commands.ApplyFsAccountMapping;

/// <summary>یک سطر نگاشت: معین <paramref name="AccCode"/> به ردیف <paramref name="RowCode"/> از قالب <paramref name="TemplateCode"/>.</summary>
public sealed record FsMappingAssignment(string AccCode, string TemplateCode, string RowCode);

/// <summary>نتیجهٔ یک سطر: <c>applied</c> (تغییر داد)، <c>unchanged</c> (از قبل همین بود) یا <c>error</c>.</summary>
public sealed record FsMappingApplyItemResult(string AccCode, string TemplateCode, string RowCode, string Status, string? Message);

public sealed record FsMappingApplyResultDto(int AppliedCount, int ErrorCount, bool DryRun, IReadOnlyList<FsMappingApplyItemResult> Items);

/// <summary>
/// <c>POST api/fs/account-mapping/apply</c> — بخش ۴۵-و: ورود نگاشت از Excel و اعمال پیشنهاد خودکار. روی
/// <b>پیش‌نویس</b> همان قالب‌هایی کار می‌کند که نمای نگاشت برای واحد هدر نشان می‌دهد. برای هر سطر، معین به
/// انتخاب‌گر ردیف مقصد افزوده و از ردیف‌های دیگر <b>همان قالب</b> (جز ردیف‌های [D]/[C]) برداشته می‌شود — جزء
/// دقیق حذف، وگرنه <c>!کد</c> افزوده. سطر خطادار رد می‌شود و بقیه اعمال می‌شوند؛ <paramref name="DryRun"/> =
/// فقط نتیجه، بدون ذخیره.
/// </summary>
public sealed record ApplyFsAccountMappingCommand(
    FsFramework Framework,
    int Year,
    IReadOnlyList<FsMappingAssignment> Items,
    bool DryRun = false) : IRequest<FsMappingApplyResultDto>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class ApplyFsAccountMappingCommandValidator : AbstractValidator<ApplyFsAccountMappingCommand>
{
    public ApplyFsAccountMappingCommandValidator()
    {
        RuleFor(x => x.Framework).IsInEnum();
        RuleFor(x => x.Year).InclusiveBetween(1300, 1600);
        RuleFor(x => x.Items).NotEmpty().Must(i => i.Count <= 5000).WithMessage("حداکثر ۵۰۰۰ سطر در هر بار.");
        RuleForEach(x => x.Items).ChildRules(i =>
        {
            i.RuleFor(a => a.AccCode).NotEmpty().MaximumLength(20);
            i.RuleFor(a => a.TemplateCode).NotEmpty().MaximumLength(50);
            i.RuleFor(a => a.RowCode).NotEmpty().MaximumLength(20);
        });
    }
}
