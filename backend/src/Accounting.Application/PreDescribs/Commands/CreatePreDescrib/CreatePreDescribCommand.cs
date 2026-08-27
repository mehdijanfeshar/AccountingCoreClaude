using MediatR;

namespace Accounting.Application.PreDescribs.Commands.CreatePreDescrib;

/// <summary>
/// Creates a new <c>TB_PREDESCRIB</c> row (Legacy pre-description template, mapped to the
/// Oracle table <c>TB_PREDESCRIBS</c> — plural, unlike the singular CLR type name). Carries
/// primitive fields only — the handler is responsible for constructing the Domain entity.
/// Returns the newly generated <see cref="Guid"/> ID.
///
/// <c>TB_PREDESCRIB</c> has no <c>ISDELETED</c> column, so this entity gets Create/Update only
/// — no Delete command exists. See <c>Accounting.Api.Controllers.PreDescribsController</c> XML
/// doc for the full rationale.
/// </summary>
/// <param name="AccountId">Optional link to <c>TB_ACCOUNTCODE</c> (<c>FK_ACCOUNT</c>).</param>
/// <param name="Descrip">Description text (max 200 chars).</param>
/// <param name="VahedCode">Optional organizational unit code (max 4 chars).</param>
/// <param name="FlagVoucher">FLAGVOUCHER column — Oracle comment "head=0 Detail=1".</param>
public sealed record CreatePreDescribCommand(
    Guid? AccountId,
    string? Descrip,
    string? VahedCode,
    bool? FlagVoucher) : IRequest<Guid>;
