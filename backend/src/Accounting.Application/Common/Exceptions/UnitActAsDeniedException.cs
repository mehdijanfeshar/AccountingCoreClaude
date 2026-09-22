namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// The caller asked to act as an organizational unit that is not theirs and not inside their
/// accessible subtree.
///
/// <para>
/// <b>Deliberately separate from <see cref="UnitAccessDeniedException"/>.</b> That one means "this
/// specific row belongs to another unit" — it is about a record, and its message carries a
/// resource name and an id. This one is about the request's <i>scope</i> and happens before any
/// row is looked at. Both map to 403, but collapsing them would make "you asked to be a unit you
/// are not" indistinguishable in logs from "you reached for someone else's record", which are very
/// different things to investigate. This mirrors the existing split between
/// <c>MissingVahedScopeException</c> and <see cref="UnitAccessDeniedException"/>.
/// </para>
///
/// <para>
/// The reference project's equivalent is <c>BusinessUserAccess.HaveAccessToUnit</c> throwing
/// «کاربر مورد نظر دسترسی لازم به اطلاعات واحد {0} را ندارد».
/// </para>
/// </summary>
public sealed class UnitActAsDeniedException : Exception
{
    public UnitActAsDeniedException(string requestedVahedCode, string callerVahedCode)
        : base($"Caller of unit '{callerVahedCode}' may not act as unit '{requestedVahedCode}'.")
    {
        RequestedVahedCode = requestedVahedCode;
        CallerVahedCode = callerVahedCode;
    }

    public string RequestedVahedCode { get; }

    public string CallerVahedCode { get; }

    /// <summary>
    /// Names the requested unit but never the caller's own. The requested code came from the
    /// caller in the first place, so echoing it reveals nothing; it is also the one piece of
    /// information that makes the error actionable ("you picked a unit you cannot use").
    /// </summary>
    public string PublicDetail => $"دسترسی به واحد «{RequestedVahedCode}» برای شما مجاز نیست.";
}
