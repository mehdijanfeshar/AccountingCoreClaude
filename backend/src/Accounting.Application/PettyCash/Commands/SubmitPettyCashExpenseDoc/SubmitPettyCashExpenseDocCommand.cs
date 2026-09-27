using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.SubmitPettyCashExpenseDoc;

/// <summary>
/// <c>POST api/petty-cash/expense-docs/{id}/submit</c> — moves a صورت‌هزینه from
/// <see cref="Accounting.Domain.ValueObjects.PettyCashDocState.Draft"/> or
/// <see cref="Accounting.Domain.ValueObjects.PettyCashDocState.Returned"/> to
/// <see cref="Accounting.Domain.ValueObjects.PettyCashDocState.New"/>. Takes no body — like
/// <c>WhiteAndBlackListsController.Blacklist</c>, what happens is entirely determined by the
/// document's current state, not a caller choice.
/// </summary>
public sealed record SubmitPettyCashExpenseDocCommand(Guid Id) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
