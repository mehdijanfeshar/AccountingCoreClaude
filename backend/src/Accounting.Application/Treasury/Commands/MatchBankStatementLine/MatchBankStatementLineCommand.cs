using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Commands.MatchBankStatementLine;

/// <summary><c>POST api/treasury/statements/{id}/lines/{lineId}/match</c> — manual match against
/// one دفتری <c>TB_VOUCHERSDETAIL</c> row.</summary>
public sealed record MatchBankStatementLineCommand(
    Guid StatementId, Guid LineId, Guid VoucherDetailId) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
