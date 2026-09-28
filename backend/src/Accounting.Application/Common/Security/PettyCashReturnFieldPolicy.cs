using Accounting.Application.Common.Exceptions;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Common.Security;

/// <summary>
/// The single place «کدام فیلدهای صورت‌هزینهٔ برگشتی، بر اساس دلیل برگشت، قابل ویرایش‌اند» lives
/// (تکمیل بخش ۲، ۲۰۲۶-۰۹-۲۸، «قفل فیلدبه‌فیلد»، صفحهٔ ۸ پاورپوینت) — same "one rule, one home"
/// shape as <see cref="PettyCashDocEditability"/>.
///
/// <b>The rule:</b> <see cref="PettyCashReturnReason.AttachmentIncomplete"/> → no form field at
/// all (attachments only); <see cref="PettyCashReturnReason.ExpenseAccountIncorrect"/> →
/// <c>expenseId</c>; <see cref="PettyCashReturnReason.AmountMismatch"/> → <c>amountBeforeTax</c>,
/// <c>vatAmount</c>, <c>invoiceNo</c>, <c>invoiceDate</c>, <c>evidenceType</c>;
/// <see cref="PettyCashReturnReason.DescriptionNeedsClarification"/> → <c>description</c>;
/// <see cref="PettyCashReturnReason.Other"/> → every field (represented as
/// <see langword="null"/> — "no restriction" — throughout this class, matching the API contract's
/// <c>editableFields: null</c> meaning "همه/قاعدهٔ عادی"). When a document was returned for more
/// than one reason, the union of each reason's fields is allowed.
///
/// Field names are the exact camelCase body names of
/// <c>Accounting.Application.PettyCash.Commands.UpdatePettyCashExpenseDoc.UpdatePettyCashExpenseDocCommand</c>
/// (plus the synthetic name <c>"attachments"</c>, handled separately by
/// <see cref="CanEditAttachments"/> since attachments are not an Update body field) — never a
/// Legacy column name, so a caller-facing message never leaks schema.
/// </summary>
public static class PettyCashReturnFieldPolicy
{
    private static readonly IReadOnlyDictionary<PettyCashReturnReason, string[]> ReasonFields =
        new Dictionary<PettyCashReturnReason, string[]>
        {
            [PettyCashReturnReason.AttachmentIncomplete] = Array.Empty<string>(),
            [PettyCashReturnReason.ExpenseAccountIncorrect] = new[] { "expenseId" },
            [PettyCashReturnReason.AmountMismatch] =
                new[] { "amountBeforeTax", "vatAmount", "invoiceNo", "invoiceDate", "evidenceType" },
            [PettyCashReturnReason.DescriptionNeedsClarification] = new[] { "description" },
        };

    /// <summary>Parses <c>TB_PC_DOC_EVENT.RETURN_REASONS</c> (comma-separated
    /// <see cref="PettyCashReturnReason"/> codes) into a list of ints. Unparseable segments are
    /// dropped rather than throwing — same defensive shape as
    /// <c>PettyCashController.ParseStates</c>. Returns an empty list for
    /// <see langword="null"/>/blank input.</summary>
    public static IReadOnlyList<int> ParseReasonCodes(string? returnReasonsCsv)
    {
        if (string.IsNullOrWhiteSpace(returnReasonsCsv))
        {
            return Array.Empty<int>();
        }

        return returnReasonsCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(segment => int.TryParse(segment, out _))
            .Select(segment => int.Parse(segment))
            .ToList();
    }

    private static bool IncludesOther(IReadOnlyCollection<int> reasonCodes) =>
        reasonCodes.Contains((int)PettyCashReturnReason.Other);

    /// <summary>Whether the caller may add/remove a <c>TB_PC_ATTACHMENT</c> row right now — only
    /// when <see cref="PettyCashReturnReason.AttachmentIncomplete"/> or
    /// <see cref="PettyCashReturnReason.Other"/> is among the reasons.</summary>
    public static bool CanEditAttachments(IReadOnlyCollection<int> reasonCodes) =>
        reasonCodes.Contains((int)PettyCashReturnReason.AttachmentIncomplete) || IncludesOther(reasonCodes);

    /// <summary>
    /// The union of every editable form-field name across <paramref name="reasonCodes"/>, or
    /// <see langword="null"/> when unrestricted — either because <see cref="PettyCashReturnReason.Other"/>
    /// is among the reasons, or because there are no parseable reasons at all (fail-open: a
    /// document in an unexpected data state falls back to the normal Draft/Returned editability
    /// rule rather than locking every field).
    /// </summary>
    public static IReadOnlyList<string>? GetEditableFields(IReadOnlyCollection<int> reasonCodes)
    {
        if (reasonCodes.Count == 0 || IncludesOther(reasonCodes))
        {
            return null;
        }

        var fields = new HashSet<string>(StringComparer.Ordinal);

        foreach (var code in reasonCodes)
        {
            if (Enum.IsDefined(typeof(PettyCashReturnReason), code) &&
                ReasonFields.TryGetValue((PettyCashReturnReason)code, out var reasonFields))
            {
                foreach (var field in reasonFields)
                {
                    fields.Add(field);
                }
            }
        }

        return fields.ToList();
    }

    /// <summary>Throws <see cref="PettyCashReturnFieldLockedException"/> listing every name in
    /// <paramref name="changedFieldNames"/> that <paramref name="reasonCodes"/> does not permit.
    /// Does nothing when every changed field is allowed, or when unrestricted (see
    /// <see cref="GetEditableFields"/>).</summary>
    public static void EnsureFieldsAllowed(
        Guid expenseDocId,
        IReadOnlyCollection<string> changedFieldNames,
        IReadOnlyCollection<int> reasonCodes)
    {
        var allowed = GetEditableFields(reasonCodes);

        if (allowed is null || changedFieldNames.Count == 0)
        {
            return;
        }

        var disallowed = changedFieldNames.Where(f => !allowed.Contains(f, StringComparer.Ordinal)).ToList();

        if (disallowed.Count > 0)
        {
            throw new PettyCashReturnFieldLockedException(expenseDocId, disallowed);
        }
    }
}
