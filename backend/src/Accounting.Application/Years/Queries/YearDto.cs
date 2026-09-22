namespace Accounting.Application.Years.Queries;

/// <summary>
/// Read-side projection of <c>TB_YEAR</c> — the financial years offered by the
/// «تغییر سال مالی و واحد» dialog.
///
/// <para>
/// ⚠️ <b>Deliberately NOT the reference project's year contract.</b> The old Angular client read
/// <c>{ id, describtion, isCurrent }</c> from <c>TbYear/GetAll</c>. Our <c>TB_YEAR</c> has no
/// <c>ID</c> and no <c>DESCRIBTION</c> column at all — its primary key <i>is</i> the year number
/// (<c>PK_TBYEAR</c> on <c>WORKING_YEAR</c>). So <see cref="WorkingYear"/> is both the identity
/// and the label, and inventing a surrogate id here would be a fiction the database cannot back.
/// </para>
/// </summary>
/// <param name="WorkingYear">WORKING_YEAR column — the Jalali year (e.g. 1405) and the table's primary key. <c>short</c>, not <c>byte</c>: the physical column is NUMBER(4) — see TB_YEAR.</param>
/// <param name="IsCurrent">
/// ISCURRENT column. Nullable in Legacy, and exposed as nullable rather than coerced to false:
/// "not marked" and "explicitly not current" are different states, and no row should be silently
/// promoted or demoted by this projection.
/// </param>
/// <param name="LastNumber">LAST_NUMBER column — last allocated voucher number for the year.</param>
public sealed record YearDto(
    short WorkingYear,
    bool? IsCurrent,
    decimal? LastNumber);
