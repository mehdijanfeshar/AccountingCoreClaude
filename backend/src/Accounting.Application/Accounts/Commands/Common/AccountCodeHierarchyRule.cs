using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Accounts.Commands.Common;

/// <summary>
/// سلسله‌مراتب کدینگ (تأیید صاحب پروژه ۲۰۲۶-۱۰-۰۷، محل: Application نه Domain — تصمیم معماری ۲):
/// گروه والد ندارد، والد «کل» گروه است و والد «معین» کل، و کد فرزند با کد والد شروع می‌شود.
/// سیستم قدیم هیچ‌کدام را کنترل نمی‌کرد (<c>centralaccount-business-reference</c> §۱).
/// روی دادهٔ توسعه نوع والد همه‌جا درست بود ولی پیشوند کد در ۴ حساب نه (مثل معین 005000 زیر کل 1022)؛
/// برای همین در ویرایش فقط وقتی کنترل می‌شود که نوع، والد یا کد عوض شود.
/// </summary>
internal static class AccountCodeHierarchyRule
{
    public static async Task EnsureAsync(
        IAccountCodeRepository repository,
        TypeCodes? typeCode,
        Guid? parentId,
        string? accCode,
        CancellationToken cancellationToken)
    {
        if (typeCode is null)
            return;

        if (typeCode == TypeCodes.Group)
        {
            if (parentId is not null)
                throw new BusinessRuleException("حساب «گروه» سطح اول کدینگ است و نمی‌تواند والد داشته باشد.");
            return;
        }

        var expectedParent = typeCode == TypeCodes.Kol ? TypeCodes.Group : TypeCodes.Kol;
        var expectedName = expectedParent == TypeCodes.Group ? "گروه" : "کل";
        var ownName = typeCode == TypeCodes.Kol ? "کل" : "معین";

        if (parentId is null)
            throw new BusinessRuleException($"حساب «{ownName}» باید زیر یک حساب «{expectedName}» باشد.");

        var parent = await repository.GetForUpdateAsync(parentId.Value, cancellationToken);
        if (parent is null || parent.ISDELETED == true)
            throw new BusinessRuleException("حساب والد انتخاب‌شده وجود ندارد یا حذف شده است.");

        if (parent.TYPECODE != expectedParent)
            throw new BusinessRuleException($"والد حساب «{ownName}» باید از نوع «{expectedName}» باشد.");

        var parentCode = parent.ACCCODE?.Trim();
        var code = accCode?.Trim();
        if (!string.IsNullOrEmpty(parentCode) && !string.IsNullOrEmpty(code)
            && (code.Length <= parentCode.Length || !code.StartsWith(parentCode, StringComparison.Ordinal)))
        {
            throw new BusinessRuleException(
                $"کد حساب باید با کد والد ({parentCode}) شروع شود و از آن بلندتر باشد.");
        }
    }
}
