namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown by <c>POST statements/{id}/import</c> when no <c>IBankStatementFileParser</c>
/// implementation is registered — خزانه‌داری، بخش ۴-د (owner decision ۲۰۲۶-۰۹-۲۹: the bank "disk"
/// file format will be supplied later; until then this is the only outcome of calling import).
/// <b>409.</b>
/// </summary>
public sealed class TreasuryBankStatementParserNotConfiguredException : Exception
{
    public TreasuryBankStatementParserNotConfiguredException()
        : base("No IBankStatementFileParser is registered.")
    {
    }

    public string PublicDetail => "قالب فایل دیسکت بانک هنوز تعریف نشده است.";
}
