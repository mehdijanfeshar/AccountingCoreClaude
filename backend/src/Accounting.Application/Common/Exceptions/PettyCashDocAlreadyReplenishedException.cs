namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown by <c>CreatePettyCashReplenishmentCommandHandler</c> immediately before inserting each
/// <c>TB_CHARGE_LINK_COST</c> row, when that صورت‌هزینه already has a non-deleted link to some
/// ترمیم — «هیچ سندی دو بار ترمیم نمی‌شود» (بخش ۳-الف، <c>docs/tankhah-khazaneh-module.md</c>).
/// Catches the race between two concurrent ترمیم requests over the same preview snapshot (there is
/// no DB unique constraint backing this — see <c>IChargeAndCostRepository.ExistsActiveLinkForCostAsync</c>
/// XML doc).
/// </summary>
public sealed class PettyCashDocAlreadyReplenishedException : Exception
{
    public PettyCashDocAlreadyReplenishedException(Guid expenseDocId)
        : base($"Petty-cash expense document {expenseDocId} is already linked to another ترمیم.")
    {
        ExpenseDocId = expenseDocId;
    }

    public Guid ExpenseDocId { get; }

    public string PublicDetail => "یک یا چند صورت‌هزینهٔ انتخاب‌شده هم‌زمان توسط ترمیم دیگری منظور شده‌اند؛ لطفاً دوباره تلاش کنید.";
}
