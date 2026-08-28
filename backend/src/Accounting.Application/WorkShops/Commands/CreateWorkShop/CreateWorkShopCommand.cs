using MediatR;

namespace Accounting.Application.WorkShops.Commands.CreateWorkShop;

/// <summary>
/// Creates a new <c>TB_WORKSHOP</c> row (Legacy workshop/production-line master). Carries
/// primitive fields only — the handler is responsible for constructing the Domain entity.
/// Returns the newly generated <see cref="Guid"/> ID.
///
/// <c>AccountCodeId</c> is backed by a real, required FK (<c>FK_WORK_ACCOUNTCODE</c> to
/// <c>TB_ACCOUNTCODE</c>); <c>BranchId</c> is backed by a real, optional FK
/// (<c>FK_WORK_VAHEDINFO</c> to <c>TB_VAHED_INFO</c>). Both are mapped centrally to 400 by
/// <c>UnitOfWork.SaveChangesAsync</c> on violation.
///
/// ⚠️ <c>IsActive</c> is a separate, non-nullable <c>NUMBER(1)</c> flag distinct from
/// <c>ISDELETED</c>, and it participates in the UNIQUE key (<c>UK_WORKSHOP</c> on
/// <c>WORKSHOPCODE, ISACTIVE, VAHEDCODE</c>). It is genuinely writable — not an audit/delete
/// column. Flagged (not fixed) here: <c>NUMBER(1)</c> columns in this schema have a track
/// record of actually being multi-valued enums rather than booleans (CLAUDE.md phase 12 —
/// <c>TB_TAFSILI.ISACTIVE</c> is the confirmed dangerous case, where <c>2</c> means inactive
/// and <c>0</c> is not in the enum at all). <c>TB_WORKSHOP.ISACTIVE</c> is an unverified
/// sibling of that known bug — the CLR type is deliberately left as <see cref="bool"/> here;
/// re-typing it is a separate, not-yet-made decision.
///
/// ⚠️ <c>CheckFile</c> maps to an Oracle <c>BLOB</c> with no size limit enforced at this layer.
/// An upload/request-size policy is an unmade decision — no limit is invented here.
/// </summary>
/// <param name="AccountCodeId">Required link to <c>TB_ACCOUNTCODE</c> (<c>FK_WORK_ACCOUNTCODE</c>).</param>
/// <param name="BranchId">Optional link to <c>TB_VAHED_INFO</c> (<c>FK_WORK_VAHEDINFO</c>).</param>
/// <param name="WorkShopName">Workshop name (required, max 100 chars).</param>
/// <param name="WorkShopCode">Workshop code (required, max 10 chars; part of <c>UK_WORKSHOP</c>).</param>
/// <param name="VahedCode">Organizational unit code (required, max 4 chars; part of <c>UK_WORKSHOP</c>).</param>
/// <param name="IsActive">ISACTIVE column — non-nullable flag, part of <c>UK_WORKSHOP</c>; see the unverified-enum note above.</param>
/// <param name="CheckFile">Optional Oracle BLOB (JSON base64); no size limit enforced here.</param>
public sealed record CreateWorkShopCommand(
    Guid AccountCodeId,
    Guid? BranchId,
    string WorkShopName,
    string WorkShopCode,
    string VahedCode,
    bool IsActive,
    byte[]? CheckFile) : IRequest<Guid>;
