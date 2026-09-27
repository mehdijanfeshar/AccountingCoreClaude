using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.StartReviewPettyCashExpenseDoc;

/// <summary>
/// <c>POST api/petty-cash/expense-docs/{id}/start-review</c> — moves a صورت‌هزینه from
/// <see cref="Accounting.Domain.ValueObjects.PettyCashDocState.New"/> to
/// <see cref="Accounting.Domain.ValueObjects.PettyCashDocState.PendingReview"/>. Takes no body;
/// only an active <c>TB_PC_REVIEWER</c> for the document's fund (who is not its own creator) may
/// call this — see <c>Accounting.Application.PettyCash.Commands.Common.IPettyCashReviewAuthorizer</c>.
/// </summary>
public sealed record StartReviewPettyCashExpenseDocCommand(Guid Id) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
