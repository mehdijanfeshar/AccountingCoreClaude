namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// A Create/Update on <c>TB_PC_FUND</c> whose <c>CODE</c> already matches another non-deleted
/// تنخواه row for the same unit. This is an application-level duplicate check performed before
/// insert/update (unlike <see cref="DuplicateKeyException"/>, which wraps an Oracle ORA-00001
/// unique-constraint violation caught after the fact) — it gives a clean, attributable 409 instead
/// of the generic one <c>UK_PC_FUND_CODE</c> would otherwise produce.
/// </summary>
public sealed class PettyCashFundCodeDuplicateException : Exception
{
    public PettyCashFundCodeDuplicateException(string code)
        : base($"An active petty-cash fund already exists with code '{code}'.")
    {
        Code = code;
    }

    public string Code { get; }

    public string PublicDetail => $"تنخواه فعالی با کد «{Code}» از قبل در این واحد ثبت شده است.";
}
