namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// هویت کار پس‌زمینه (بدون درخواست HTTP)، مثل اجرای روزانهٔ زمان‌بندی تعهدی. Scoped: کار پس‌زمینه در scope خودش
/// <see cref="UserId"/> را می‌گذارد و <see cref="ICurrentUser"/> وقتی درخواست HTTP نیست همین را برمی‌گرداند
/// (ADDUSERID سند و لاگ تغییرات). در درخواست HTTP هرگز خوانده نمی‌شود.
/// </summary>
public sealed class SystemActorContext
{
    public string? UserId { get; set; }
}
