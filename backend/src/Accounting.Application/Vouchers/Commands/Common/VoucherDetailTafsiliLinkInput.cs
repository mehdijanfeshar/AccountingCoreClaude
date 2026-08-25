namespace Accounting.Application.Vouchers.Commands.Common;

/// <summary>
/// One <c>TB_VOUCHERDETAIL_LINK_TAFSILI</c> row (a تفصیلی assignment on a voucher detail line),
/// carried as part of its parent <c>TB_VOUCHERSDETAIL</c> write — never on its own. Shared by
/// <see cref="Accounting.Application.Vouchers.Commands.CreateVoucherDetail.CreateVoucherDetailCommand"/>
/// and <see cref="Accounting.Application.Vouchers.Commands.UpdateVoucherDetail.UpdateVoucherDetailCommand"/>,
/// which is why it lives in a shared <c>Commands/Common</c> folder rather than inside either
/// command's own folder (unlike the single-consumer
/// <c>CreateVoucherHead.CreateVoucherHeadDetailInput</c> it is otherwise modelled on).
///
/// <b>This is NOT independent CRUD for <c>TB_VOUCHERDETAIL_LINK_TAFSILI</c>.</b> The team rule
/// (2026-08-20, recorded in <c>docs/tamin-core-entity-reference.md</c> بخش ۵) that every
/// <c>*_LINK_TAFSIL*</c>/<c>*_LINK_LEVEL*</c> table stays permanently embedded is untouched: this
/// record is a transport DTO nested inside the parent aggregate's command, there is no
/// <c>ITafsiliLinkRepository</c>, no controller, and no MediatR request of its own — exactly the
/// shape <c>NoIndependentLinkTableWritePathTests</c> locks in. It closes the gap that phase 10
/// recorded as an open item: the links could previously only ever be <i>deleted</i> (by cascade),
/// never written.
///
/// Primitives only — same documented, deliberate exception to the phase-5 "Command carries only
/// primitives" rule already granted to <c>CreateVoucherHeadDetailInput</c>: a nested
/// <c>sealed record</c> of primitives is not a Domain entity, so the rule's actual purpose
/// (never smuggle an EF-tracked entity across the API boundary) is respected.
///
/// Deliberately carries NO <c>VahedCode</c>/<c>Year</c>, and no <c>VoucherDetailId</c>: all three
/// are derived by the handler from the parent detail row being created or updated in the very
/// same call. This structurally prevents a link that disagrees with its own parent about which
/// unit/year/line it belongs to — the same reasoning that kept those fields off
/// <c>CreateVoucherHeadDetailInput</c>. It also carries no <c>Id</c>: link row identity is
/// generated server-side and is never addressable by the caller (a caller identifies a link by
/// its <see cref="TafsiliId"/>/<see cref="LevelId"/> pair, which is what the update reconcile
/// below matches on).
/// </summary>
/// <param name="TafsiliId">TAFSILI_ID column — the assigned تفصیلی. Required (the column is non-nullable).</param>
/// <param name="LevelId">LEVEL_ID column — the تفصیلی level this assignment sits at. Required (the column is non-nullable).</param>
public sealed record VoucherDetailTafsiliLinkInput(
    Guid TafsiliId,
    Guid LevelId);
