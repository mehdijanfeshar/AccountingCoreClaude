namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown by write-side command handlers (<c>UpdateAccountCodeCommandHandler</c>,
/// <c>UpdateVoucherHeadCommandHandler</c>, <c>DeleteAccountCodeCommandHandler</c>,
/// <c>DeleteVoucherHeadCommandHandler</c>, and any future handler that must mutate an
/// existing row) when the requested row does not exist — either because the <c>ID</c> was
/// never valid, or because it is soft-deleted (<c>ISDELETED = true</c>), which this
/// application treats as "logically absent" for update purposes, consistent with the
/// <c>ISDELETED != true</c> filter already used by the read repositories.
///
/// This is an Application-level exception: it carries no Oracle/EF Core types and no
/// table/column names, so <c>Accounting.Api</c> can map it to <c>404 Not Found</c> via
/// <c>GlobalExceptionHandler</c> without leaking any Legacy schema detail in the response
/// body.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string resourceName, Guid id)
        : base($"{resourceName} with id '{id}' was not found.")
    {
    }
}
