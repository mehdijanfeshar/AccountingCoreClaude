using FluentValidation;

namespace Accounting.Application.Vouchers.Commands.Common;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints for
/// <c>TB_VOUCHERDETAIL_LINK_TAFSILI</c> in <c>LegacyDbContext</c>. Per the recorded "Legacy fully
/// replaces the rich model" architecture decision, accounting invariants must NOT be re-created
/// here — in particular this validator does NOT check that the تفصیلی is <i>permitted</i> for the
/// line's account (the <c>TB_ACCOUNTCODE → TB_ACCOUNT_LINK_TAFSILGROUP → TB_TAFSIL_LINK_TAFSILGROUP
/// → TB_TAFSILI</c> chain), nor that a تفصیلی is <i>required</i>: both were deliberately dropped
/// invariants and remain open items in <c>CLAUDE.md</c>.
///
/// Unlike the all-optional FK fields on <c>CreateVoucherHeadDetailInput</c>, both fields here DO
/// get <c>NotEmpty</c>: <c>TAFSILI_ID</c> and <c>LEVEL_ID</c> are non-nullable <see cref="Guid"/>
/// columns, so <c>Guid.Empty</c> is not an "absent FK" — it is a value that would be written to
/// the database as a real, meaningless key.
///
/// Note that <c>NotEmpty</c> only rejects the all-zero <see cref="Guid"/>; it cannot verify that
/// the referenced <c>TB_TAFSILI</c> row exists. A non-existent-but-well-formed id is caught at
/// persistence time by Oracle and surfaces as a 400 via the ORA-02291 →
/// <c>ForeignKeyViolationException</c> translation in <c>UnitOfWork.SaveChangesAsync</c> — except
/// that <c>TB_VOUCHERDETAIL_LINK_TAFSILI.TAFSILI_ID</c>/<c>LEVEL_ID</c> have <b>no FK constraint
/// at all</b> in the Legacy schema (its only FK is to <c>TB_VOUCHERSDETAIL</c>), a gap already
/// recorded as an open item in <c>CLAUDE.md</c> and deliberately not closed here.
/// </summary>
public sealed class VoucherDetailTafsiliLinkInputValidator : AbstractValidator<VoucherDetailTafsiliLinkInput>
{
    public VoucherDetailTafsiliLinkInputValidator()
    {
        RuleFor(x => x.TafsiliId)
            .NotEmpty();

        RuleFor(x => x.LevelId)
            .NotEmpty();
    }
}
