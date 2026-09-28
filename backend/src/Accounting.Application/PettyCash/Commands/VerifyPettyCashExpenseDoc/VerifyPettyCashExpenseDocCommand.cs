using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.VerifyPettyCashExpenseDoc;

/// <summary>
/// <c>POST api/petty-cash/expense-docs/{id}/verify</c> — «تأیید کنترل» توسط بازرس، اولین گام از
/// تأیید دومرحله‌ای (تکمیل بخش ۲، ۲۰۲۶-۰۹-۲۸، صفحهٔ ۱۲ پاورپوینت). فقط در
/// <see cref="Accounting.Domain.ValueObjects.PettyCashDocState.PendingReview"/>، فقط
/// <see cref="Accounting.Domain.ValueObjects.PettyCashRole.Inspector"/> همان تنخواه، و فقط یک‌بار
/// در هر چرخهٔ بررسی. <b>وضعیت سند تغییر نمی‌کند</b> — فقط
/// <c>TB_PC_EXPENSE_DOC.VERIFIED_BY_USERID</c>/<c>VERIFIED_DATE</c> ست می‌شوند و یک ردیف
/// <c>TB_PC_DOC_EVENT</c> (اکشن <see cref="Accounting.Domain.ValueObjects.PettyCashDocAction.Verify"/>،
/// <c>FROM_STATE == TO_STATE == PendingReview</c>) ثبت می‌شود.
/// </summary>
public sealed record VerifyPettyCashExpenseDocCommand(Guid Id, string? Note) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
