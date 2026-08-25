namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown by the write side (via <c>IUnitOfWork.SaveChangesAsync</c> implementations in
/// <c>Accounting.Infrastructure</c>) when persistence fails because a foreign key in the row
/// being written points at a parent row that does not exist — Oracle <b>ORA-02291</b>
/// ("integrity constraint violated - parent key not found"). Real examples in this schema:
/// <c>FK_VOUCHERDETAIL_ACCOUNCODE</c> (<c>TB_VOUCHERSDETAIL.ACCOUNT_ID</c> →
/// <c>TB_ACCOUNTCODE</c>), <c>FK_VOUCHERDETAIL_RECEIP</c>, and <c>FK_VOUCHERHEAD</c>.
///
/// Exact sibling of <see cref="DuplicateKeyException"/> in every structural respect — same
/// Application-level positioning (carries no Oracle/EF Core types, so <c>Accounting.Api</c> can
/// map it to an HTTP status without depending on Oracle), same rule that the preserved
/// <see cref="Exception.InnerException"/> (a <c>DbUpdateException</c> wrapping an
/// <c>OracleException</c>) is for logging only and must never reach an HTTP response body —
/// because it would leak Legacy table/column/constraint names to the caller.
///
/// <b>Why this maps to 400 and not 409</b> (recorded here so the choice is not re-litigated):
/// a duplicate key (ORA-00001 → <see cref="DuplicateKeyException"/> → 409) is a genuine conflict
/// with the current state of the target resource — the row the caller wants to create already
/// exists, and the caller can reconcile and resubmit. ORA-02291 is different in kind: the caller
/// supplied an identifier that references nothing, which is an invalid request payload rather
/// than a state conflict. 404 was also rejected: the request's target resource (the collection
/// endpoint) does exist, and 404 already carries a distinct, narrower meaning on these routes —
/// the parent voucher head is missing, raised deliberately by
/// <c>CreateVoucherDetailCommandHandler</c>'s explicit pre-check, never by this exception.
///
/// <b>Scope — deliberately ORA-02291 only.</b> Its sibling ORA-02292 ("child record found",
/// raised when deleting a parent that still has children) is NOT translated, because this project
/// performs no hard deletes at all — every delete path is a soft delete
/// (<c>ISDELETED = true</c>), which no FK can object to. Mapping it would be speculation about a
/// code path that does not exist.
/// </summary>
public sealed class ForeignKeyViolationException : Exception
{
    public ForeignKeyViolationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
