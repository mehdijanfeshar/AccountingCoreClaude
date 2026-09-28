namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown by <c>CreatePettyCashReplenishmentCommandHandler</c> when a تنخواه has zero currently
/// تأییدشده صورت‌هزینه rows still unlinked to any ترمیم — there is nothing to build a ترمیم from
/// (بخش ۳-الف، <c>docs/tankhah-khazaneh-module.md</c>: «اگر هیچ سندی نیست ⇒ ۴۰۹»).
/// </summary>
public sealed class PettyCashNoDocumentsToReplenishException : Exception
{
    public PettyCashNoDocumentsToReplenishException(Guid fundId)
        : base($"Petty-cash fund {fundId} has no unlinked approved documents to replenish.")
    {
        FundId = fundId;
    }

    public Guid FundId { get; }

    public string PublicDetail => "هیچ صورت‌هزینهٔ تأییدشدهٔ منتظر ترمیمی برای این تنخواه وجود ندارد.";
}
