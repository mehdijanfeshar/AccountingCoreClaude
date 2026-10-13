using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Queries.Treasury;

/// <summary>One row of <c>GET api/treasury/roles</c>.</summary>
public sealed record TreasuryRoleDto(
    Guid Id,
    string UserId,
    string? UserName,
    TreasuryRole Role);
