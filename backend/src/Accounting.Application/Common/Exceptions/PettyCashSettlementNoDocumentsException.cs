namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// No صورت‌هزینه row is currently eligible for this settlement (no
/// <see cref="Accounting.Domain.ValueObjects.PettyCashDocState.Approved"/> document with
/// register date on or before the period's <c>PERIOD_END</c>) — بخش ۳-ب
/// (<c>docs/tankhah-khazaneh-module.md</c> section 9): "حداقل یک سند تأییدشده". 409, same
/// "nothing to act on" shape as <c>PettyCashNoDocumentsToReplenishException</c>.
/// </summary>
public sealed class PettyCashSettlementNoDocumentsException : Exception
{
    public PettyCashSettlementNoDocumentsException(Guid fundId)
        : base($"Fund {fundId} has no Approved صورت‌هزینه eligible for settlement.")
    {
        FundId = fundId;
    }

    public Guid FundId { get; }

    public string PublicDetail => "هیچ صورت‌هزینهٔ تأییدشده‌ای برای تسویه در این دوره وجود ندارد.";
}
