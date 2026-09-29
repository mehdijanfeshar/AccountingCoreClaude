using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Queries.GetTreasurySetting;

/// <summary><c>GET api/treasury/settings</c>.</summary>
public sealed record GetTreasurySettingQuery : IRequest<TreasurySettingDto?>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
