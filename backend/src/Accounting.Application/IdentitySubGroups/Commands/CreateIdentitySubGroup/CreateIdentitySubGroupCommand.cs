using MediatR;

namespace Accounting.Application.IdentitySubGroups.Commands.CreateIdentitySubGroup;

/// <summary>
/// Creates a new <c>TB_IDENTITYSUBGRP</c> row (Legacy "identity"/شناسنامه sub-group definition,
/// mapped to Oracle table <c>TB_IDENTITYSUBGRPS</c> — note the singular CLR type name vs. the
/// plural table name). Carries primitive fields only — the handler is responsible for
/// constructing the Domain entity. Returns the newly generated <see cref="Guid"/> ID.
///
/// Uniqueness: the combination <c>(VahedCode, Year, IdentySubGroupsCode)</c> is enforced by the
/// Oracle constraint <c>AK_AK_IDENTYSUBGRPS_IDENTYSU</c>. No pre-check is performed here — a
/// duplicate combination surfaces as an Oracle ORA-00001, already translated centrally by
/// <c>UnitOfWork.SaveChangesAsync</c> into a
/// <see cref="Accounting.Application.Common.Exceptions.DuplicateKeyException"/> → 409.
/// </summary>
/// <param name="IdentyGroupsId">
/// IDENTYGROUPS_ID column (note the missing "I" — the real Oracle column name is spelled this
/// way, intentionally preserved rather than silently corrected) — required FK to
/// <c>TB_IDENTITYGROUP</c> (constraint <c>FK_IDENTYSU_IDENTYGR</c>). No pre-check is performed:
/// an id that does not reference an existing <c>TB_IDENTITYGROUP</c> row surfaces as an Oracle
/// ORA-02291, already translated centrally by <c>UnitOfWork.SaveChangesAsync</c> into a
/// <see cref="Accounting.Application.Common.Exceptions.ForeignKeyViolationException"/> → 400.
/// </param>
/// <param name="SubgrpsDesc">SUBGRPS_DESC column (max 100 chars, required).</param>
/// <param name="SubgrpsLen">SUBGRPS_LEN column (<c>NUMBER(2)</c>, mapped as <c>byte</c>, required).</param>
/// <param name="SumFlag">SUMFLAG column (<c>NUMBER(1)</c>, mapped as non-nullable <c>bool</c>).</param>
/// <param name="Fixed">FIXED column (<c>NUMBER(1)</c>, mapped as non-nullable <c>bool</c>).</param>
/// <param name="SubgrpsType">
/// SUBGRPS_TYPE column (<c>NUMBER(1)</c>, mapped as nullable <c>bool</c>). ⚠️ CONFIRMED-SUSPICIOUS:
/// the Oracle column comment itself reads "نوع : حروف, اعداد, يا هردو" (type: letters, digits, or
/// both) — i.e. a <b>three</b>-valued enum, which a <c>bool</c>/<c>bool?</c> structurally cannot
/// represent. This matches the project-wide <c>bool?</c>/<c>NUMBER(1)</c> scaffolding bug pattern
/// recorded in CLAUDE.md's Phase 12 (19 columns across 13 tables; only
/// <c>TB_ACCOUNTCODE.TYPECODE</c>, <c>TB_VOUCHERSHEAD.DOCLIFE</c> and <c>TB_TAFSILI.ISACTIVE</c>
/// were triaged there). This specific column was NOT previously in that list — flagging it here
/// as a newly-confirmed instance for the same follow-up task. Modeled as <c>bool?</c> exactly as
/// the current Domain entity declares it; fixing the underlying CLR type is a separate,
/// out-of-scope task and was deliberately not attempted here.
/// </param>
/// <param name="VahedCode">
/// VAHEDCODE column (max 4 chars, required — participates in <c>AK_AK_IDENTYSUBGRPS_IDENTYSU</c>).
/// </param>
/// <param name="Year">
/// YEAR column (max 4 chars, required fiscal year — participates in
/// <c>AK_AK_IDENTYSUBGRPS_IDENTYSU</c>).
/// </param>
/// <param name="IdentySubGroupsCode">
/// IDENTYSUBGROUPS_CODE column (max 2 chars, optional — participates in
/// <c>AK_AK_IDENTYSUBGRPS_IDENTYSU</c>).
/// </param>
public sealed record CreateIdentitySubGroupCommand(
    Guid IdentyGroupsId,
    string SubgrpsDesc,
    byte SubgrpsLen,
    bool SumFlag,
    bool Fixed,
    bool? SubgrpsType,
    string VahedCode,
    string Year,
    string? IdentySubGroupsCode) : IRequest<Guid>;
