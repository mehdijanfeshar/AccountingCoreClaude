using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Commands.Expenses.Common;

/// <summary>
/// حساب هزینه (<c>TB_EXPENCE.ACCOUNTCODE_ID</c>) فقط از سطح «معین» — صاحب پروژه ۲۰۲۶-۱۰-۲۶. صورت‌هزینهٔ
/// تنخواه و سند تسویه روی همین حساب ثبت می‌شوند و سند روی گروه/کل ثبت نمی‌شود. پیکر فرم هم فقط معین
/// نشان می‌دهد، ولی قاعده اینجا هم هست (تصمیم معماری ۱۰).
/// </summary>
internal static class ExpenseAccountRule
{
    public static async Task EnsureMoinAsync(
        IAccountCodeRepository repository,
        Guid? accountCodeId,
        CancellationToken cancellationToken)
    {
        if (accountCodeId is null)
            return;

        var account = await repository.GetForUpdateAsync(accountCodeId.Value, cancellationToken);
        if (account is null || account.ISDELETED == true)
            throw new BusinessRuleException("حساب انتخاب‌شده برای هزینه وجود ندارد یا حذف شده است.");

        if (account.TYPECODE != TypeCodes.Moin)
            throw new BusinessRuleException("حساب هزینه باید از سطح «معین» باشد، نه گروه یا کل.");
    }
}
