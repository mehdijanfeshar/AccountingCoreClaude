namespace Accounting.Domain.ValueObjects;

/// <summary>
/// State machine for <c>TB_TR_BANK_STATEMENT_LINE.MATCH_STATE</c> — خزانه‌داری، بخش ۴-د
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰، ۲۰۲۶-۰۹-۲۹).
/// <see cref="Unmatched"/> → (<c>auto-match</c>) <see cref="AutoMatched"/> or (<c>match</c>)
/// <see cref="ManualMatched"/>, or (<c>resolve</c>) <see cref="Resolved"/> directly.
/// <see cref="AutoMatched"/>/<see cref="ManualMatched"/> → (<c>unmatch</c>) back to
/// <see cref="Unmatched"/>. <see cref="Resolved"/> → (<c>unresolve</c>, only while the resolution
/// voucher — if any — is still temporary) back to <see cref="Unmatched"/>.
/// </summary>
public enum BankStatementLineMatchState
{
    Unmatched = 1,
    AutoMatched = 2,
    ManualMatched = 3,
    Resolved = 4,
}
