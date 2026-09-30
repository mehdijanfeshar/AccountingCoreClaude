using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace Accounting.Application.FinancialStatements.Commands.TransitionFsRun;

/// <summary>
/// <c>POST api/fs/runs/{id}/transitions</c> — گردش تأیید اجرا (بخش ۴۵-ه، سند منبع §۱۱):
/// <list type="bullet">
/// <item><b>Submit</b>: پیش‌نویس ⇒ در بازبینی. اجرای «آزمایشی» (قالب پیش‌نویس) یا با کنترل مسدودکنندهٔ ناموفق ⇒ ۴۰۹.</item>
/// <item><b>Approve</b>: در بازبینی ⇒ تأییدشده. تهیه‌کننده یا ارسال‌کننده ⇒ ۴۰۳ (تفکیک وظایف).</item>
/// <item><b>Return</b>: در بازبینی/تأییدشده ⇒ پیش‌نویس، با دلیل اجباری.</item>
/// <item><b>Publish</b>: تأییدشده ⇒ منتشرشده؛ تهیه‌کننده ⇒ ۴۰۳. منتشرشدهٔ قبلی همان دوره «جایگزین‌شده» می‌شود.</item>
/// </list>
/// ⚠️ نقش‌ها (بازبین، مدیرکل، معاون) هنوز تعریف نشده‌اند — هر کاربر واحد جز تهیه‌کننده/ارسال‌کننده می‌تواند
/// تأیید کند (<c>docs/fs-module.md</c> §۱۱). پاسخ = وضعیت تازه.
/// </summary>
public sealed record TransitionFsRunCommand(Guid Id, FsRunAction Action, string? Comments) : IRequest<FsRunState>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class TransitionFsRunCommandValidator : AbstractValidator<TransitionFsRunCommand>
{
    public TransitionFsRunCommandValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
        RuleFor(x => x.Action)
            .Must(a => a is FsRunAction.Submit or FsRunAction.Approve or FsRunAction.Return or FsRunAction.Publish)
            .WithMessage("اقدام نامعتبر است.");
        RuleFor(x => x.Comments).MaximumLength(1000);
        RuleFor(x => x.Comments).NotEmpty().When(x => x.Action == FsRunAction.Return).WithMessage("دلیل برگشت لازم است.");
    }
}
