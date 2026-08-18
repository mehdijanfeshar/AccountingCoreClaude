namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown by the write side (via <c>IUnitOfWork.SaveChangesAsync</c> implementations in
/// <c>Accounting.Infrastructure</c>) when persistence fails because of a unique-constraint
/// violation — e.g. <c>UK_ACCOUNTCODE</c> on <c>TB_ACCOUNTCODE.ACCCODE</c> or
/// <c>UK_VOUCHERHEAD_NUMBER</c> on <c>TB_VOUCHERSHEAD (DOC_NUM, YEAR, VAHEDCODE)</c>.
///
/// This is an Application-level exception: it carries no Oracle/EF Core types so that
/// <c>Accounting.Api</c> can catch it and map it to <c>409 Conflict</c> without taking any
/// dependency on Oracle. The original low-level exception (<c>DbUpdateException</c> wrapping
/// an <c>OracleException</c>) is preserved as <see cref="Exception.InnerException"/> for
/// logging only — it must never be surfaced in an HTTP response body.
/// </summary>
public sealed class DuplicateKeyException : Exception
{
    public DuplicateKeyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
