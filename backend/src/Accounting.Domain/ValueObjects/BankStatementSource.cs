namespace Accounting.Domain.ValueObjects;

/// <summary>
/// Where a <c>TB_TR_BANK_STATEMENT</c> row's lines came from — خزانه‌داری، بخش ۴-د
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰، ۲۰۲۶-۰۹-۲۹ owner decision). <see cref="Import"/>
/// is a pluggable seam (<c>IBankStatementFileParser</c>) with no implementation registered yet —
/// the owner will supply the bank "disk" file format later.
/// </summary>
public enum BankStatementSource
{
    Manual = 1,
    Import = 2,
}
