using System.Reflection;

namespace Accounting.Domain.Tests.TestSupport;

/// <summary>
/// کمکِ تست: تمام موجودیت‌های دامنه (AccountGroup, DetailAccountType, ...) دارای
/// <c>public int Id { get; private set; }</c> هستند که فقط توسط EF Core (در زمان
/// Materialization از دیتابیس) پر می‌شود. چون در این پروژهٔ تست هیچ دیتابیسی وجود ندارد،
/// هر موجودیتی که مستقیماً از طریق Create/factory ساخته شود همیشه <c>Id == 0</c> باقی
/// می‌ماند. این یک مسئلهٔ تست‌پذیری واقعی است: اگر دو <see cref="Accounting.Domain.Entities.DetailAccountType"/>
/// متفاوت هر دو Id=0 داشته باشند، از دید منطق دامنه (که تشخیص «نوع تفصیلی» را صرفاً بر
/// اساس Id انجام می‌دهد؛ رجوع به SubsidiaryAccount.LinkDetailType و SubsidiaryDetailPolicy)
/// این دو نوع «یکی» به‌حساب می‌آیند و سناریوهایی مثل «نوع تفصیلی لینک‌نشده» قابل تست نیستند.
///
/// این کلاس با Reflection مقدار Id را مستقیماً از طریق private setter ست می‌کند تا بتوان
/// موجودیت‌های متمایز با Idهای متمایز برای تست ساخت — دقیقاً شبیه‌سازی همان کاری که EF Core
/// در دنیای واقعی انجام می‌دهد. این صرفاً یک ابزار کمکی تست است و هیچ تغییری در کد
/// backend/src/Accounting.Domain اعمال نمی‌کند.
/// </summary>
public static class EntityIdAssigner
{
    public static T WithId<T>(T entity, int id)
    {
        var property = typeof(T).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"نوع {typeof(T).Name} پراپرتی Id ندارد.");

        var setMethod = property.GetSetMethod(nonPublic: true)
            ?? throw new InvalidOperationException($"پراپرتی Id در نوع {typeof(T).Name} متد ست (حتی private) ندارد.");

        setMethod.Invoke(entity, new object[] { id });
        return entity;
    }
}
