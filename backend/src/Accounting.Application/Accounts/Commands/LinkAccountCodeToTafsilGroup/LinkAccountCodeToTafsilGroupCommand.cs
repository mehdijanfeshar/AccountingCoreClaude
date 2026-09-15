using MediatR;

namespace Accounting.Application.Accounts.Commands.LinkAccountCodeToTafsilGroup;

/// <summary>
/// Creates a new <c>TB_ACCOUNT_LINK_TAFSILGROUP</c> row — "ارتباط معین با گروه تفصیلی" in the
/// legacy Angular app: links a معین (<see cref="AccountCodeId"/>) to a گروه تفصیلی
/// (<see cref="TafsilGroupId"/>) for one تفصیلی level (<see cref="LevelId"/>).
///
/// <b>Parent-scoped by design, not an independent Command for an embedded table.</b>
/// <c>TB_ACCOUNT_LINK_TAFSILGROUP</c> is one of the project's permanently-embedded
/// <c>*_LINK_TAFSIL*</c> tables (<c>docs/open-decisions.md</c>, enforced by
/// <c>NoIndependentLinkTableWritePathTests</c>) — this Command lives under <c>Accounts</c> (the
/// معین aggregate's own namespace) and is only ever reachable through
/// <c>AccountCodesController</c>'s nested <c>tafsil-group-links</c> routes, exactly mirroring the
/// phase-11 <c>IVoucherDetailRepository.AddTafsiliLinkAsync</c> precedent for
/// <c>TB_VOUCHERDETAIL_LINK_TAFSILI</c>.
///
/// Uniqueness: the composite <c>(ACCOUNT_ID, LEVEL_ID, TAFSILGROUP_ID)</c> is enforced by the
/// Oracle constraint <c>UK_ACCOUNTLINKTAFSILGROUP</c> — a معین may have several گروه‌های تفصیلی
/// linked for the same level, just never the exact same triple twice. No pre-check is
/// performed — a duplicate surfaces as an Oracle ORA-00001, translated centrally by
/// <c>UnitOfWork.SaveChangesAsync</c> into a
/// <see cref="Accounting.Application.Common.Exceptions.DuplicateKeyException"/> → 409.
///
/// All three id fields are backed by real, required FKs (<c>FK_TAFSILGOUP_ACCOUNTCODE</c>,
/// <c>FK_TBACCOUNTTAFSILGROUP_LEVEL</c>, <c>FK_TBACCOUNTLINKE_TAFSILGROUP</c>) — an invalid id in
/// any of the three is mapped centrally to 400 by <c>UnitOfWork.SaveChangesAsync</c>.
///
/// Not Vahed-scoped: <c>TB_ACCOUNT_LINK_TAFSILGROUP</c> has no <c>VAHEDCODE</c> column.
/// </summary>
/// <param name="AccountCodeId">TB_ACCOUNTCODE.ID — the معین (bound from the route, never the body).</param>
/// <param name="LevelId">TB_LEVEL_TAFSIL.ID — the تفصیلی level this link applies to.</param>
/// <param name="TafsilGroupId">TB_TAFSIL_GROUP.ID — the گروه تفصیلی being linked.</param>
public sealed record LinkAccountCodeToTafsilGroupCommand(
    Guid AccountCodeId,
    Guid LevelId,
    Guid TafsilGroupId) : IRequest<Guid>;
