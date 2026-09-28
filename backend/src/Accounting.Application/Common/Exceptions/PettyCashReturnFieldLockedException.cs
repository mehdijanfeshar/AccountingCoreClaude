namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown by <c>Accounting.Application.Common.Security.PettyCashReturnFieldPolicy</c> when an
/// Update (or attachment add/delete) on a
/// <see cref="Accounting.Domain.ValueObjects.PettyCashDocState.Returned"/> صورت‌هزینه touches a
/// field the document's most recent <c>Return</c> event's reasons do not permit — تکمیل بخش ۲
/// (۲۰۲۶-۰۹-۲۸، «قفل فیلدبه‌فیلد»، صفحهٔ ۸ پاورپوینت).
///
/// <b>409, not 400</b> — same state-based-refusal shape as
/// <see cref="PettyCashDocNotEditableException"/>: the caller IS allowed to edit this document
/// right now, just not these particular fields, given why it was returned.
/// </summary>
public sealed class PettyCashReturnFieldLockedException : Exception
{
    public PettyCashReturnFieldLockedException(Guid expenseDocId, IReadOnlyList<string> fieldNames)
        : base(
            $"Petty-cash expense document {expenseDocId}'s return-reason field lock forbids " +
            $"changing: {string.Join(", ", fieldNames)}.")
    {
        ExpenseDocId = expenseDocId;
        FieldNames = fieldNames;
    }

    public Guid ExpenseDocId { get; }

    public IReadOnlyList<string> FieldNames { get; }

    public string PublicDetail =>
        $"با توجه به دلیل برگشت سند، تغییر این فیلدها مجاز نیست: {string.Join("، ", FieldNames)}.";
}
