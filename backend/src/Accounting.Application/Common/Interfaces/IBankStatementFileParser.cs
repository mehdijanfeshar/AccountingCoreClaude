namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// One parsed line from an imported bank "disk" (دیسکت) statement file, before it becomes a
/// <see cref="Accounting.Domain.Entity.TB_TR_BANK_STATEMENT_LINE"/> — خزانه‌داری، بخش ۴-د
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). Same field shape as the manual-entry command
/// (شمسی <c>YYYYMMDD</c> date, exactly one of <see cref="Withdrawal"/>/<see cref="Deposit"/> &gt; 0).
/// </summary>
public sealed record ParsedBankStatementLine(
    string LineDate,
    string? BankReference,
    string? Description,
    decimal Withdrawal,
    decimal Deposit,
    decimal? Balance);

/// <summary>
/// Pluggable seam for reading a bank's own "disk" (دیسکت) statement export — خزانه‌داری، بخش ۴-د
/// (owner decision ۲۰۲۶-۰۹-۲۹: the exact file format is bank-specific and will be supplied by the
/// project owner later). <b>Deliberately no implementation is registered yet</b> — until one is,
/// <c>POST statements/{id}/import</c> resolves this via <see cref="IServiceProvider.GetService"/>
/// (not constructor injection, so its absence does not break DI composition at startup) and
/// returns <b>409</b> («قالب فایل دیسکت بانک هنوز تعریف نشده است») when none is found.
///
/// <para>
/// Plugging a real format later is exactly one new class + one DI line: implement this interface
/// (e.g. <c>SomeBankStatementFileParser</c>) and register it with
/// <c>services.AddScoped&lt;IBankStatementFileParser, SomeBankStatementFileParser&gt;()</c> in
/// <c>Accounting.Infrastructure.DependencyInjection</c> — no controller/command/handler change
/// needed, since <c>ImportBankStatementCommandHandler</c> only ever calls through this interface.
/// </para>
/// </summary>
public interface IBankStatementFileParser
{
    /// <summary>
    /// Parses <paramref name="content"/> into statement lines for <paramref name="bankAccountId"/>.
    /// Implementations own their own format-specific validation; this seam does not prescribe one.
    /// </summary>
    Task<IReadOnlyList<ParsedBankStatementLine>> ParseAsync(
        Stream content, Guid bankAccountId, CancellationToken cancellationToken = default);
}
