namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Base for the two halves of the «تفصیلی الزامی» rule, both mapped to <b>400</b> by
/// <c>GlobalExceptionHandler</c>.
///
/// <b>Unlike every other exception here, these carry a caller-visible detail string.</b> The
/// standing rule that an exception body must never leak Legacy schema names still holds — what
/// <see cref="PublicDetail"/> contains is the تفصیلی level's own business name, the same text the
/// accountant already sees on the voucher form. Telling them "سطح «مرکز هزینه» را پر کنید" is the
/// difference between a fixable error and a mystery, and it reveals nothing a user of the form
/// does not already know.
/// </summary>
public abstract class TafsiliLevelRuleException : Exception
{
    protected TafsiliLevelRuleException(string message)
        : base(message)
    {
    }

    /// <summary>The text that is safe to return in the HTTP response body.</summary>
    public abstract string PublicDetail { get; }
}

/// <summary>
/// The line's معین requires تفصیلی at one or more levels and the request did not supply them.
///
/// Rule A from <c>docs/centralaccount-business-reference.md</c> §3-3: a معین requires a تفصیلی at
/// level L exactly when a row exists in <c>TB_ACCOUNT_LINK_LEVEL</c> for (معین, L). The reference
/// project raises its equivalent (<c>LevelNotProvidedException</c>) in three independent write
/// paths; ours raises this one.
/// </summary>
public sealed class RequiredTafsiliLevelMissingException : TafsiliLevelRuleException
{
    public RequiredTafsiliLevelMissingException(Guid accountCodeId, IReadOnlyList<string> levelNames)
        : base($"Account {accountCodeId} requires تفصیلی at level(s): {string.Join(", ", levelNames)}.")
    {
        AccountCodeId = accountCodeId;
        LevelNames = levelNames;
    }

    public Guid AccountCodeId { get; }

    public IReadOnlyList<string> LevelNames { get; }

    public override string PublicDetail =>
        $"برای حساب انتخاب‌شده، تفصیلی این سطح‌ها الزامی است: {string.Join('،', LevelNames)}.";
}

/// <summary>
/// The request supplied a تفصیلی at a level the line's معین does not have configured.
///
/// Rule B, the other half — and the half that is easy to forget, because an extra value feels
/// harmless. It is not: there is no FK on <c>TB_VOUCHERDETAIL_LINK_TAFSILI.LEVEL_ID</c>, so
/// without this check a caller bypassing the form can write a تفصیلی assignment at a level that
/// means nothing for that حساب, and every report reading it back would silently disagree with the
/// configuration. The reference project's equivalent is <c>UnauthorizedLevelException</c>.
/// </summary>
public sealed class TafsiliLevelNotPermittedException : TafsiliLevelRuleException
{
    public TafsiliLevelNotPermittedException(Guid accountCodeId, Guid levelId)
        : base($"Level {levelId} is not configured for account {accountCodeId}.")
    {
        AccountCodeId = accountCodeId;
        LevelId = levelId;
    }

    public Guid AccountCodeId { get; }

    public Guid LevelId { get; }

    /// <summary>
    /// Deliberately does not name the level. A caller that sent an unconfigured level id either
    /// has stale configuration or is not going through the form at all; in neither case does
    /// echoing the id back help, and the id is not a name the accountant would recognise anyway.
    /// </summary>
    public override string PublicDetail =>
        "برای حساب انتخاب‌شده، تفصیلی در سطحی فرستاده شده که برای این حساب تعریف نشده است.";
}
