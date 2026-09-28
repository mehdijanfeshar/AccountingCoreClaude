namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown by <c>CreatePettyCashReplenishmentCommandHandler</c> as a defensive guard: «ترمیم ≤
/// سقف تنخواه» (بخش ۳-الف، <c>docs/tankhah-khazaneh-module.md</c>). In practice this can never
/// actually fire — a ترمیم's total is always the sum of currently-Approved documents, and
/// <c>PettyCashSubmitRuleChecker</c> already guarantees Approved+in-flight never exceeds
/// <c>CEILING</c> at Submit time — but it is kept as a cheap, explicit belt-and-suspenders check
/// rather than a silent assumption. 400: the amount just does not fit, same shape as
/// <c>PettyCashPerDocLimitExceededException</c>.
/// </summary>
public sealed class PettyCashReplenishmentExceedsCeilingException : Exception
{
    public PettyCashReplenishmentExceedsCeilingException(Guid fundId, decimal amount, decimal ceiling)
        : base($"Replenishment total {amount} for petty-cash fund {fundId} exceeds its ceiling {ceiling}.")
    {
        FundId = fundId;
        Amount = amount;
        Ceiling = ceiling;
    }

    public Guid FundId { get; }

    public decimal Amount { get; }

    public decimal Ceiling { get; }

    public string PublicDetail => "مبلغ ترمیم از سقف تنخواه بیشتر است.";
}
