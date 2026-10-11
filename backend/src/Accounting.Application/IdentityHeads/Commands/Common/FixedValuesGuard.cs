using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;

namespace Accounting.Application.IdentityHeads.Commands.Common;

/// <summary>
/// برنامهٔ مرجع (مقداردهی اولیهٔ ویژگی): هر جزء ثابت گروه مقدار الزامی دارد. بدون این کنترل، شناسنامه‌ای
/// بی‌مقدار ساخته می‌شد که در پنجرهٔ «اطلاعات ویژگی» سند فقط «شناسنامه ۱» دیده می‌شد.
/// </summary>
internal static class FixedValuesGuard
{
    public static async Task EnsureCompleteAsync(
        IIdentityHeadRepository repository,
        Guid identityGroupId,
        IReadOnlyCollection<IdentityHeadFixItemInput>? fixItems,
        CancellationToken cancellationToken)
    {
        var required = await repository.GetFixedSubGroupIdsAsync(identityGroupId, cancellationToken);
        var given = (fixItems ?? [])
            .Where(i => !string.IsNullOrWhiteSpace(i.Value))
            .Select(i => i.IdentitySubGroupId)
            .ToHashSet();
        if (required.Any(id => !given.Contains(id)))
        {
            throw new BusinessRuleException("مقدار همهٔ اجزای ثابت این گروه ویژگی الزامی است.");
        }
    }
}
