namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Server-side source of "where is this request coming from", used to populate
/// <c>TB_PC_DOC_EVENT.CLIENT_IP</c> without <c>Accounting.Application</c> taking any dependency
/// on ASP.NET Core (<c>HttpContext</c>/<c>IHttpContextAccessor</c> live in <c>Accounting.Api</c>
/// only — see <see cref="ICurrentUser"/> for the same separation applied to identity).
///
/// Deliberately its own small interface rather than a new member on <see cref="ICurrentUser"/>:
/// the client IP is audit metadata for one specific write path (petty-cash document events), not
/// an identity/authorization concern the way every existing <see cref="ICurrentUser"/> member is.
/// </summary>
public interface IClientInfoProvider
{
    /// <summary>
    /// The calling client's IP address as text (IPv4 or IPv6), or <see langword="null"/> when
    /// there is no current HTTP request (e.g. running outside a request scope, such as a unit
    /// test) or the underlying connection exposes no remote address.
    /// </summary>
    string? ClientIp { get; }
}
