namespace Accounting.Application.Vouchers.Queries;

/// <summary>
/// One تفصیلی assignment on a voucher line, as read back.
///
/// <para>
/// <b>Why the read side needed this.</b> A line's tafsili links could be written (create and
/// update both accept them) but never read, so nothing outside the database could see what a line
/// was actually assigned. That was survivable while the UI only created vouchers; it stops being
/// survivable the moment an edit form exists, because a form that cannot see the current links
/// has exactly two options — send <see langword="null"/> and leave them untouched even when the
/// caller changed the account they belong to, or send a list it had to guess. The first silently
/// leaves a line carrying another account's تفصیلی; the second destroys assignments.
/// </para>
///
/// <para>
/// Soft-deleted links are not returned: a caller editing a line needs the links that currently
/// apply, and a deleted one does not.
/// </para>
/// </summary>
/// <param name="TafsiliId">The تفصیلی assigned (<c>TB_VOUCHERDETAIL_LINK_TAFSILI.TAFSILI_ID</c>).</param>
/// <param name="LevelId">Which تفصیلی level it fills (<c>LEVEL_ID</c>).</param>
public sealed record VoucherDetailTafsiliLinkDto(
    Guid TafsiliId,
    Guid LevelId);
