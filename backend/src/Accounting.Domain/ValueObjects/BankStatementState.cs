namespace Accounting.Domain.ValueObjects;

/// <summary>
/// State machine for <c>TB_TR_BANK_STATEMENT.STATE</c> — خزانه‌داری، بخش ۴-د
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰، ۲۰۲۶-۰۹-۲۹). Header/line CRUD, matching and
/// resolution are only allowed while <see cref="Open"/> — <see cref="Closed"/> is a view-only
/// snapshot (reopenable, unlike a terminal voucher state).
/// </summary>
public enum BankStatementState
{
    Open = 1,
    Closed = 2,
}
