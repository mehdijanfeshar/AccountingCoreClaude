namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// The authenticated caller's token names an organizational unit that matches no
/// <c>TB_VAHED_INFO</c> row.
///
/// <para>
/// <b>Why this is loud rather than an empty list.</b> The reference project raises the equivalent
/// (<c>UnitCodeNotFoundException</c>, «کد واحد با این مشخصات یافت نشد») from
/// <c>GetVahedInfoQueryHandler</c> instead of returning an empty page, and the reason holds here:
/// an empty unit picker is indistinguishable from "you are correctly configured and simply have
/// no sub-units", so the one state an operator most needs to see — the IDP org claim and the
/// Legacy unit table having drifted apart — would be the one state the UI hides.
/// </para>
///
/// <para>
/// Distinct from <c>MissingVahedScopeException</c> on purpose: that one means "your token carries
/// no unit at all", this one means "it carries a unit we have never heard of". Collapsing them
/// would make a token wiring bug and a data mismatch look identical in logs.
/// </para>
/// </summary>
public sealed class UnknownCallerUnitException : Exception
{
    public UnknownCallerUnitException(string vahedCode)
        : base($"The authenticated caller's unit code '{vahedCode}' matches no TB_VAHED_INFO row.")
    {
        VahedCode = vahedCode;
    }

    public string VahedCode { get; }

    /// <summary>
    /// Safe to return to the caller: it names no Legacy table or column, and the unit code is the
    /// caller's own — the same value their own token carries.
    /// </summary>
    public string PublicDetail => $"کد واحد «{VahedCode}» در فهرست واحدهای سازمانی یافت نشد.";
}
