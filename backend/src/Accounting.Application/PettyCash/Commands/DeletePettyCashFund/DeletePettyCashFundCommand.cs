using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.DeletePettyCashFund;

/// <summary>
/// <c>POST api/petty-cash/funds/{fundId}/delete</c> — soft-deletes a <c>TB_PC_FUND</c> row.
/// Refused (409) while the fund still has non-deleted <c>TB_PC_EXPENSE_DOC</c> rows — see
/// <see cref="Accounting.Application.Common.Exceptions.PettyCashFundHasExpenseDocsException"/>.
/// </summary>
public sealed record DeletePettyCashFundCommand(Guid Id) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
