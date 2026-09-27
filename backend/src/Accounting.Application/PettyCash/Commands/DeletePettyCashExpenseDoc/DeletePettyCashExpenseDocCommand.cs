using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.DeletePettyCashExpenseDoc;

/// <summary>
/// <c>POST api/petty-cash/expense-docs/{id}/delete</c> — soft-deletes a صورت‌هزینه, only while it
/// is <see cref="Accounting.Domain.ValueObjects.PettyCashDocState.Draft"/>
/// (<see cref="Accounting.Application.Common.Security.PettyCashDocEditability"/>). Also soft-deletes
/// its Legacy <c>TB_CHARGEANDCOST_HEAD</c>/<c>TB_CHARGEANDCOST_DETAIL</c> pair in the same
/// transaction, so a direct query against those tables reflects the deletion too — this project
/// never issues physical deletes anywhere (CLAUDE.md).
/// </summary>
public sealed record DeletePettyCashExpenseDocCommand(Guid Id) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
