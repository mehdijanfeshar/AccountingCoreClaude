using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using Accounting.Application.AuditLogs;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Accounting.Infrastructure.Legacy;

/// <summary>
/// لاگ تغییرات (تصمیم صاحب پروژه ۲۰۲۶-۱۰-۱۲): هر ثبت، اصلاح، حذف و بازگردانی در هر جدول — با کاربر (کد ملی)، نام،
/// ساعت، IP، مقدار قبلی و جدید — در <c>TB_AUDITLOG</c>. فقط نقش «مدیر مالی» آن را می‌بیند.
/// <list type="bullet">
/// <item><b>ثبت</b>: یک ردیف، <c>NEWVALUE</c> = JSON همهٔ ستون‌های پر.</item>
/// <item><b>اصلاح</b>: یک ردیف به ازای هر ستون عوض‌شده (<c>COLUMNNAME</c>، قبلی، جدید). ستون‌های ردیابی
/// (<see cref="AuditTableCatalog.HousekeepingColumns"/>) جدا ثبت نمی‌شوند.</item>
/// <item><b>حذف</b> (نرم با <c>ISDELETED</c> یا واقعی): یک ردیف، <c>OLDVALUE</c> = JSON کل رکورد پیش از حذف.
/// <b>بازگردانی</b> (<c>ISDELETED</c> ۱←۰) هم یک ردیف با JSON کل رکورد.</item>
/// </list>
/// <para>
/// <b>Why an interceptor, and why in the same SaveChanges.</b> Like <see cref="VoucherAtfNumberInterceptor"/>, one
/// hook covers every write path, including ones added later. The log rows are added to the same unit of work,
/// so a change and its log commit or fail together: no change can land unlogged, and a failed save leaves no
/// phantom log row. ⚠️ So until DDL <c>077</c> is applied, every save fails (ORA-00904) — or set
/// <c>Audit:Enabled=false</c>.
/// </para>
/// </summary>
public sealed class AuditLogInterceptor : SaveChangesInterceptor
{
    public const string ActionInsert = "Insert";
    public const string ActionUpdate = "Update";
    public const string ActionDelete = "Delete";
    public const string ActionRestore = "Restore";

    private const string SystemUser = "SYSTEM";

    private static readonly JsonSerializerOptions Json = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private readonly IServiceProvider _services;

    public AuditLogInterceptor(IServiceProvider services)
    {
        _services = services;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is LegacyDbContext context)
            await ApplyAsync(context, cancellationToken);
        return result;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is LegacyDbContext context)
            ApplyAsync(context, CancellationToken.None).GetAwaiter().GetResult();
        return result;
    }

    private async Task ApplyAsync(LegacyDbContext context, CancellationToken cancellationToken)
    {
        context.ChangeTracker.DetectChanges();
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(e => e.Metadata.FindPrimaryKey() is not null
                        && !AuditTableCatalog.ExcludedTables.Contains(e.Metadata.GetTableName() ?? string.Empty))
            .ToList();
        if (entries.Count == 0)
            return;

        var who = await ResolveWhoAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var operationId = Guid.NewGuid();
        var rows = new List<TB_AUDITLOG>();

        foreach (var entry in entries)
        {
            var table = entry.Metadata.GetTableName()!;
            var info = AuditTableCatalog.Find(table);
            var (recordId, keyText) = KeyOf(entry);
            var label = LabelOf(entry, info) ?? keyText;
            var parentId = await ParentOfAsync(context, entry, info, cancellationToken);
            var vahed = Truncate(Text(entry, "VAHEDCODE"), 4) ?? who.VahedCode;
            var year = Truncate(Text(entry, "YEAR"), 4);

            TB_AUDITLOG Row(string action, string? column, string? oldValue, string? newValue) => new()
            {
                ID = Guid.NewGuid(),
                TABLENAME = table,
                RECORDID = recordId,
                COLUMNNAME = column,
                OLDVALUE = oldValue,
                NEWVALUE = newValue,
                ACTIONTYPE = action,
                USERID = who.UserId,
                USERFULLNAME = who.Name,
                VAHEDCODE = vahed,
                YEAR = year,
                CHANGEDATE = now,
                IPADDRESS = who.Ip,
                OPERATIONID = operationId,
                RECORDLABEL = label,
                PARENTID = parentId,
            };

            switch (entry.State)
            {
                case EntityState.Added:
                    rows.Add(Row(ActionInsert, null, null, Snapshot(entry, current: true)));
                    break;
                case EntityState.Deleted:
                    rows.Add(Row(ActionDelete, null, Snapshot(entry, current: false), null));
                    break;
                case EntityState.Modified:
                    var softDelete = SoftDeleteTransition(entry);
                    if (softDelete is true)
                    {
                        rows.Add(Row(ActionDelete, null, Snapshot(entry, current: false), null));
                        break;
                    }

                    if (softDelete is false)
                    {
                        rows.Add(Row(ActionRestore, null, null, Snapshot(entry, current: true)));
                        break;
                    }

                    foreach (var p in entry.Properties)
                    {
                        if (!p.IsModified || p.Metadata.IsPrimaryKey()
                            || AuditTableCatalog.HousekeepingColumns.Contains(ColumnOf(p)))
                            continue;
                        var oldValue = Format(p.OriginalValue);
                        var newValue = Format(p.CurrentValue);
                        if (oldValue != newValue)
                            rows.Add(Row(ActionUpdate, ColumnOf(p), oldValue, newValue));
                    }

                    break;
            }
        }

        context.AddRange(rows);
    }

    private async Task<(string UserId, string? Name, string? VahedCode, string? Ip)> ResolveWhoAsync(CancellationToken cancellationToken)
    {
        var user = _services.GetService<ICurrentUser>();
        var ip = Truncate(_services.GetService<IClientInfoProvider>()?.ClientIp, 45);
        if (user is null || !user.IsAuthenticated)
            return (SystemUser, null, null, ip);

        string? vahed = user.VahedCode;
        try
        {
            // واحدی که کاربر با هدر X-Vahed-Code در آن کار می‌کند (اعتبارسنجی‌شده)؛ برای جدول‌های بدون VAHEDCODE.
            if (_services.GetService<IUnitScopeResolver>() is { } resolver)
                vahed = await resolver.ResolveEffectiveVahedCodeAsync(cancellationToken);
        }
        catch (Exception)
        {
            // هدر نامعتبر نباید ذخیره‌ای را که خودش مجاز بوده بشکند؛ واحد خود کاربر ثبت می‌شود.
        }

        return (user.UserId, Truncate(user.DisplayName, 200), Truncate(vahed, 4), ip);
    }

    /// <summary>true = حذف نرم (ISDELETED ۰←۱)، false = بازگردانی، null = اصلاح معمولی.</summary>
    private static bool? SoftDeleteTransition(EntityEntry entry)
    {
        var p = entry.Properties.FirstOrDefault(x => ColumnOf(x) == "ISDELETED");
        if (p is null || !p.IsModified)
            return null;
        var was = IsTrue(p.OriginalValue);
        var now = IsTrue(p.CurrentValue);
        return was == now ? null : now;
    }

    private static bool IsTrue(object? value) => value switch
    {
        bool b => b,
        null => false,
        _ => Convert.ToDecimal(value, CultureInfo.InvariantCulture) != 0,
    };

    private static (Guid Id, string? Text) KeyOf(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey()!.Properties;
        var values = key.Select(k => entry.Property(k.Name).CurrentValue).ToList();
        if (values.Count == 1 && values[0] is Guid g)
            return (g, null);
        return (Guid.Empty, Truncate(string.Join(" / ", values.Select(Format)), 200));
    }

    private static string? LabelOf(EntityEntry entry, AuditTableInfo? info)
    {
        if (info is null || info.LabelColumns.Count == 0)
            return null;
        var parts = info.LabelColumns.Select(c => Text(entry, c)).Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
        return parts.Count == 0 ? null : Truncate(string.Join(" - ", parts), 200);
    }

    /// <summary>شناسهٔ والدِ بالاترین سطح (مثلاً تفصیلی ردیف ← ردیف ← سرسند).</summary>
    private static async Task<Guid?> ParentOfAsync(
        LegacyDbContext context, EntityEntry entry, AuditTableInfo? info, CancellationToken cancellationToken)
    {
        Guid? parent = null;
        for (var depth = 0; info?.ParentColumn is not null && depth < 3; depth++)
        {
            var value = ValueOf(entry, info.ParentColumn);
            if (value is not Guid id || id == Guid.Empty)
                break;
            parent = id;

            var parentInfo = info.ParentTable is null ? null : AuditTableCatalog.Find(info.ParentTable);
            if (parentInfo?.ParentColumn is null)
                break;
            var parentType = context.Model.GetEntityTypes().FirstOrDefault(t => t.GetTableName() == info.ParentTable);
            if (parentType is null)
                break;
            var parentEntity = await context.FindAsync(parentType.ClrType, [id], cancellationToken);
            if (parentEntity is null)
                break;
            entry = context.Entry(parentEntity);
            info = parentInfo;
        }

        return parent;
    }

    private static string? Snapshot(EntityEntry entry, bool current)
    {
        var values = new SortedDictionary<string, string?>(StringComparer.Ordinal);
        foreach (var p in entry.Properties)
        {
            var column = ColumnOf(p);
            if (AuditTableCatalog.HousekeepingColumns.Contains(column))
                continue;
            var formatted = Format(current ? p.CurrentValue : p.OriginalValue);
            if (formatted is not null)
                values[column] = formatted;
        }

        return JsonSerializer.Serialize(values, Json);
    }

    private static string ColumnOf(PropertyEntry p) => p.Metadata.GetColumnName() ?? p.Metadata.Name;

    private static object? ValueOf(EntityEntry entry, string column)
        => entry.Properties.FirstOrDefault(p => ColumnOf(p) == column)?.CurrentValue;

    private static string? Text(EntityEntry entry, string column) => Format(ValueOf(entry, column))?.Trim();

    private static string? Format(object? value) => value switch
    {
        null => null,
        string s => s,
        DateTime d => d.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
        DateTimeOffset d => d.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture),
        bool b => b ? "1" : "0",
        Enum e => $"{Convert.ToInt64(e, CultureInfo.InvariantCulture)} ({e})",
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        byte[] bytes => $"[{bytes.Length} بایت]",
        _ => value.ToString(),
    };

    private static string? Truncate(string? value, int max)
        => value is null ? null : value.Length <= max ? value : value[..max];
}
