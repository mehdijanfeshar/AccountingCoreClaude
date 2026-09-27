using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.ReturnPettyCashExpenseDoc;

/// <summary>
/// <c>POST api/petty-cash/expense-docs/{id}/return</c> — moves a صورت‌هزینه from
/// <see cref="Accounting.Domain.ValueObjects.PettyCashDocState.PendingReview"/> to
/// <see cref="Accounting.Domain.ValueObjects.PettyCashDocState.Returned"/>.
/// </summary>
/// <param name="ReasonCodes">One or more <see cref="Accounting.Domain.ValueObjects.PettyCashReturnReason"/>
/// values — persisted comma-separated on <c>TB_PC_DOC_EVENT.RETURN_REASONS</c>.</param>
/// <param name="Deadline"><c>YYYYMMDD</c> Jalali, must be a date after today — stamped onto
/// <c>TB_PC_EXPENSE_DOC.RETURN_DEADLINE</c>. No system-generated default (design doc, تصمیم‌های
/// بخش ۲): the reviewer supplies it explicitly on every Return call.</param>
public sealed record ReturnPettyCashExpenseDocCommand(
    Guid Id,
    IReadOnlyList<int> ReasonCodes,
    string Deadline,
    string? Note) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
