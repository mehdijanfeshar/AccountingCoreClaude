using MediatR;

namespace Accounting.Application.RevolvingFunds.Commands.CreateRevolvingFund;

/// <summary>
/// Creates a new <c>TB_REVOLVING_FUND</c> row (Legacy revolving fund / تنخواه master). Carries
/// primitive fields only — the handler is responsible for constructing the Domain entity.
/// Returns the newly generated <see cref="Guid"/> ID.
///
/// <c>AccountCodeId</c> is backed by a real, optional FK (<c>FK_ACCOUNTCODE_REVOLVING</c> to
/// <c>TB_ACCOUNTCODE</c>), mapped centrally to 400 by <c>UnitOfWork.SaveChangesAsync</c> on
/// violation. <c>(Code, VahedCode, Year)</c> together are protected by the real UNIQUE
/// constraint <c>UK_REVOLVING_CODE</c>, mapped centrally to 409.
///
/// ⚠️ <c>Code</c> is only 2 characters wide (<c>CODE</c> column) — get
/// <see cref="CreateRevolvingFundCommandValidator"/>'s <c>MaximumLength(2)</c> right.
///
/// ⚠️ This table's child <c>TB_REVOLVINGFUND_LINK_TAFSILI</c> is permanently embedded per team
/// rule — no repository, command, or cascade for it exists here, by design.
/// </summary>
/// <param name="Code">CODE column (required, max 2 chars; part of <c>UK_REVOLVING_CODE</c>).</param>
/// <param name="Name">NAME column (required, max 200 chars).</param>
/// <param name="Description">DESCRIPTION column (optional, max 100 chars).</param>
/// <param name="DefaultAmount">DEFAULTAMOUNT column (optional, <c>NUMBER(25)</c>).</param>
/// <param name="AccountCodeId">Optional link to <c>TB_ACCOUNTCODE</c> (<c>FK_ACCOUNTCODE_REVOLVING</c>).</param>
/// <param name="VahedCode">VAHEDCODE column (optional, max 4 chars; part of <c>UK_REVOLVING_CODE</c>).</param>
/// <param name="Year">YEAR column (optional, max 4 chars; part of <c>UK_REVOLVING_CODE</c>).</param>
public sealed record CreateRevolvingFundCommand(
    string Code,
    string Name,
    string? Description,
    decimal? DefaultAmount,
    Guid? AccountCodeId,
    string? VahedCode,
    string? Year) : IRequest<Guid>;
