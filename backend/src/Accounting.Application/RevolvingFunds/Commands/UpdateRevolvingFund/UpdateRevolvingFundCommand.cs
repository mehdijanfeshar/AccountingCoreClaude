using MediatR;

namespace Accounting.Application.RevolvingFunds.Commands.UpdateRevolvingFund;

/// <summary>
/// Fully replaces the writable fields of an existing <c>TB_REVOLVING_FUND</c> row (PUT
/// semantics, not PATCH) — the same replace-vs-patch rationale as <c>UpdateWorkShopCommand</c>
/// applies here.
///
/// Deliberately excludes <c>ID</c>, <c>ADDUSERID</c>, <c>CREATEDDATE</c> and <c>ISDELETED</c>:
/// identity and creation audit are immutable after insert, and <c>ISDELETED</c> is owned
/// exclusively by <c>DeleteRevolvingFundCommand</c>. <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> are
/// likewise absent because the handler sources them from
/// <see cref="Accounting.Application.Common.Interfaces.ICurrentUser"/> and the server clock,
/// never from client input.
/// </summary>
/// <param name="Id">The <c>TB_REVOLVING_FUND.ID</c> to update (bound from the route, never the body).</param>
/// <param name="Code">CODE column (required, max 2 chars; part of <c>UK_REVOLVING_CODE</c>).</param>
/// <param name="Name">NAME column (required, max 200 chars).</param>
/// <param name="Description">DESCRIPTION column (optional, max 100 chars).</param>
/// <param name="DefaultAmount">DEFAULTAMOUNT column (optional, <c>NUMBER(25)</c>).</param>
/// <param name="AccountCodeId">Optional link to <c>TB_ACCOUNTCODE</c> (<c>FK_ACCOUNTCODE_REVOLVING</c>).</param>
/// <param name="VahedCode">VAHEDCODE column (optional, max 4 chars; part of <c>UK_REVOLVING_CODE</c>).</param>
/// <param name="Year">YEAR column (optional, max 4 chars; part of <c>UK_REVOLVING_CODE</c>).</param>
public sealed record UpdateRevolvingFundCommand(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    decimal? DefaultAmount,
    Guid? AccountCodeId,
    string? VahedCode,
    string? Year) : IRequest;
