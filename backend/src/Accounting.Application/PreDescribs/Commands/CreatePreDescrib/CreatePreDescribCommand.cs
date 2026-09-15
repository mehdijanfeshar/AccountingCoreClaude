using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
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
/// <param name="FlagVoucher">FLAGVOUCHER column — Oracle comment "head=0 Detail=1".</param>
public sealed record CreatePreDescribCommand(
    Guid? AccountId,
    string? Descrip,
    bool? FlagVoucher) : IRequest<Guid>, IVahedScopedCommand
{
    /// <summary>
    /// Organizational unit code (<c>VAHEDCODE</c> column — nullable at the Legacy schema level,
    /// but always populated with a real value here). Never bound from the request body —
    /// <see cref="JsonIgnoreAttribute"/> keeps it out of both model binding and the Swagger
    /// schema — and never trusted even if a caller manages to set it: <c>VahedScopeBehavior</c>
    /// unconditionally overwrites this with the authenticated caller's own unit code before the
    /// request reaches <c>CreatePreDescribCommandHandler</c>. See <see cref="IVahedScopedCommand"/>
    /// for the full mechanism.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
