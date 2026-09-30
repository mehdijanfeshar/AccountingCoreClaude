using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.FinancialStatements.Commands.DeleteFsRun;

/// <summary>
/// <c>POST api/fs/runs/{id}/delete</c> — حذف نرم اجرا. در ۴۵-ب همهٔ اجراها پیش‌نویس‌اند؛ از ۴۵-د اجرای
/// تأییدشده/منتشرشده حذف نمی‌شود. اجرای واحد دیگر = ۴۰۴.
/// </summary>
public sealed record DeleteFsRunCommand(Guid Id) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
