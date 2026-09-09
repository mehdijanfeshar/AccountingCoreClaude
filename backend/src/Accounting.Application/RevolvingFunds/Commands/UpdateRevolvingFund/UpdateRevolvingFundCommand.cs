using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
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
///
/// ⚠️ <b>Scope note:</b> <see cref="IVahedScopedCommand"/> here only guarantees that
/// <c>VAHEDCODE</c> cannot be *changed* to an arbitrary unit by the caller. It does
/// <b>not</b> check whether the caller is allowed to touch this particular row in the first
/// place — record-ownership verification on Update (i.e. "does this <c>Id</c> already belong to
/// the caller's unit?") is explicitly out of scope for this pass, by project-owner decision. The
/// IDOR risk on direct-by-id access therefore remains open for Update; only the "what unit does
/// this row end up in" half of the problem is closed here.
/// </summary>
/// <param name="Id">The <c>TB_REVOLVING_FUND.ID</c> to update (bound from the route, never the body).</param>
/// <param name="Code">CODE column (required, max 2 chars; part of <c>UK_REVOLVING_CODE</c>).</param>
/// <param name="Name">NAME column (required, max 200 chars).</param>
/// <param name="Description">DESCRIPTION column (optional, max 100 chars).</param>
/// <param name="DefaultAmount">DEFAULTAMOUNT column (optional, <c>NUMBER(25)</c>).</param>
/// <param name="AccountCodeId">Optional link to <c>TB_ACCOUNTCODE</c> (<c>FK_ACCOUNTCODE_REVOLVING</c>).</param>
/// <param name="Year">YEAR column (optional, max 4 chars; part of <c>UK_REVOLVING_CODE</c>).</param>
public sealed record UpdateRevolvingFundCommand(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    decimal? DefaultAmount,
    Guid? AccountCodeId,
    string? Year) : IRequest, IVahedScopedCommand
{
    /// <summary>
    /// Organizational unit code (<c>VAHEDCODE</c> column — nullable at the Legacy schema level,
    /// but always populated with a real value here; part of <c>UK_REVOLVING_CODE</c>). Never
    /// bound from the request body — <see cref="JsonIgnoreAttribute"/> keeps it out of both model
    /// binding and the Swagger schema — and never trusted even if a caller manages to set it:
    /// <c>VahedScopeBehavior</c> unconditionally overwrites this with the authenticated caller's
    /// own unit code before the request reaches <c>UpdateRevolvingFundCommandHandler</c>. See
    /// <see cref="IVahedScopedCommand"/> for the full mechanism, and the scope note above for
    /// what this does <b>not</b> cover.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
