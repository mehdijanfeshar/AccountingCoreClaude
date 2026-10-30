using System.Text.Json;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Accounting.Infrastructure.Tests.Legacy;

/// <summary>
/// لاگ تغییرات (فاز ۵۷): یک ردیف <c>TB_AUDITLOG</c> برای هر ستون تغییریافته، درج/حذف با Snapshot کامل،
/// حذف/بازگردانی نرم به‌جای Update خام، زنجیرهٔ PARENTID، و حالت انبوه (<c>BulkAuditLabel</c>).
/// </summary>
public sealed class AuditLogInterceptorTests : IDisposable
{
    private const string CreateTablesSql = """
        CREATE TABLE TB_VOUCHERSHEAD (
            ID TEXT PRIMARY KEY, DOC_NUM TEXT, DATE_DOC TEXT, DOCLIFE INTEGER,
            HEAD_DESC TEXT, APENDIX TEXT, SYSTEM_TYPE TEXT, FLAG_STATE TEXT,
            CREATEDDATE TEXT, UPDATEDDATE TEXT, ADDUSERID TEXT, CHANGEUSERID TEXT,
            VAHEDCODE TEXT, YEAR TEXT, ISDELETED INTEGER, ATTACHFILE BLOB,
            ATTACHFILE_NAME TEXT, ATF_NUM TEXT, ISAUTOMATIC INTEGER,
            SNDVAHEDCODE TEXT, PARENTHEAD_ID TEXT, GLOBALNUMBER TEXT
        );
        CREATE TABLE TB_VOUCHERSDETAIL (
            ID TEXT PRIMARY KEY, ACCOUNT_ID TEXT, RECEIP_ID TEXT, CHECK_ID TEXT,
            LOWLEVELCODE_ID TEXT, ETEBAR_ID TEXT, DESCRIPTION TEXT, RADIF INTEGER,
            DEBTOR TEXT, CREDITOR TEXT, CREATEDDATE TEXT, UPDATEDDATE TEXT,
            ADDUSERID TEXT, CHANGEUSERID TEXT, VAHEDCODE TEXT, ISDELETED INTEGER,
            VOUCHERSHEAD_ID TEXT, YEAR TEXT
        );
        CREATE TABLE TB_VOUCHERDETAIL_LINK_TAFSILI (
            ID TEXT PRIMARY KEY, VOUCHERSDETAIL_ID TEXT, TAFSILI_ID TEXT, LEVEL_ID TEXT,
            CREATEDDATE TEXT, UPDATEDDATE TEXT, ADDUSERID TEXT, CHANGEUSERID TEXT,
            ISDELETED INTEGER, VAHEDCODE TEXT, YEAR TEXT
        );
        CREATE TABLE TB_VAHED_INFO (
            ID TEXT PRIMARY KEY, VAHEDCODE TEXT, VAHEDNAME TEXT, CITY_ID TEXT,
            VAHEDTYPE_ID TEXT, PARENT_ID TEXT
        );
        CREATE TABLE TB_AUDITLOG (
            ID TEXT PRIMARY KEY, TABLENAME TEXT, RECORDID TEXT, COLUMNNAME TEXT,
            OLDVALUE TEXT, NEWVALUE TEXT, ACTIONTYPE TEXT, USERID TEXT,
            VAHEDCODE TEXT, YEAR TEXT, CHANGEDATE TEXT, IPADDRESS TEXT,
            OPERATIONID TEXT, USERFULLNAME TEXT, RECORDLABEL TEXT, PARENTID TEXT
        );
        """;

    private readonly SqliteConnection _connection;
    private readonly FakeCurrentUser _user = new();
    private readonly FakeClientInfo _clientInfo = new();

    public AuditLogInterceptorTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        using var setup = CreateContext();
        setup.Database.ExecuteSqlRaw(CreateTablesSql);
    }

    public void Dispose() => _connection.Dispose();

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public bool IsAuthenticated { get; set; } = true;
        public string UserId { get; set; } = "1234567890";
        public string? DisplayName { get; set; } = "کاربر آزمایشی";
        public string? VahedCode { get; set; } = "0042";
        public string? RequestedVahedCode => null;
        public bool IsInRole(string role) => false;
    }

    private sealed class FakeClientInfo : IClientInfoProvider
    {
        public string? ClientIp { get; set; } = "10.0.0.5";
    }

    private LegacyDbContext CreateContext(bool authenticated = true)
    {
        _user.IsAuthenticated = authenticated;
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(_user);
        services.AddSingleton<IClientInfoProvider>(_clientInfo);
        var provider = services.BuildServiceProvider();

        return new LegacyDbContext(
            new DbContextOptionsBuilder<LegacyDbContext>()
                .UseSqlite(_connection)
                .AddInterceptors(new AuditLogInterceptor(provider))
                .Options);
    }

    private static TB_VAHED_INFO Vahed(string? code = "0042") => new()
    {
        ID = Guid.NewGuid(), VAHEDCODE = code!, VAHEDNAME = "واحد تست", CITY_ID = Guid.NewGuid(), VAHEDTYPE_ID = Guid.NewGuid(),
    };

    [Fact]
    public async Task Insert_writes_one_row_with_full_snapshot_in_newvalue()
    {
        await using var context = CreateContext();
        var vahed = Vahed();
        context.Set<TB_VAHED_INFO>().Add(vahed);
        await context.SaveChangesAsync();

        var log = Assert.Single(context.Set<TB_AUDITLOG>().AsNoTracking());
        Assert.Equal("TB_VAHED_INFO", log.TABLENAME);
        Assert.Equal(vahed.ID, log.RECORDID);
        Assert.Equal(AuditLogInterceptor.ActionInsert, log.ACTIONTYPE);
        Assert.Null(log.OLDVALUE);
        Assert.Null(log.COLUMNNAME);
        var snapshot = JsonSerializer.Deserialize<Dictionary<string, string>>(log.NEWVALUE!)!;
        Assert.Equal("0042", snapshot["VAHEDCODE"]);
        Assert.Equal("واحد تست", snapshot["VAHEDNAME"]);
        Assert.Equal("1234567890", log.USERID);
        Assert.Equal("کاربر آزمایشی", log.USERFULLNAME);
        Assert.Equal("10.0.0.5", log.IPADDRESS);
        Assert.NotNull(log.OPERATIONID);
    }

    [Fact]
    public async Task Housekeeping_columns_are_excluded_from_the_snapshot()
    {
        await using var context = CreateContext();
        var head = new TB_VOUCHERSHEAD
        {
            ID = Guid.NewGuid(), DOC_NUM = "000001", YEAR = "1404", VAHEDCODE = "0042",
            ADDUSERID = "1234567890", CREATEDDATE = DateTime.UtcNow, ISDELETED = false,
        };
        context.Set<TB_VOUCHERSHEAD>().Add(head);
        await context.SaveChangesAsync();

        var log = context.Set<TB_AUDITLOG>().AsNoTracking().Single(l => l.TABLENAME == "TB_VOUCHERSHEAD");
        var snapshot = JsonSerializer.Deserialize<Dictionary<string, string>>(log.NEWVALUE!)!;
        Assert.DoesNotContain("ADDUSERID", snapshot.Keys);
        Assert.DoesNotContain("CREATEDDATE", snapshot.Keys);
        Assert.Equal("000001", log.RECORDLABEL);
    }

    [Fact]
    public async Task Update_writes_one_row_per_changed_column_only()
    {
        TB_VAHED_INFO vahed;
        await using (var seed = CreateContext())
        {
            vahed = Vahed();
            seed.Set<TB_VAHED_INFO>().Add(vahed);
            await seed.SaveChangesAsync();
        }

        await using var context = CreateContext();
        var tracked = await context.Set<TB_VAHED_INFO>().SingleAsync(v => v.ID == vahed.ID);
        tracked.VAHEDNAME = "نام جدید";
        tracked.PARENT_ID = Guid.NewGuid();
        await context.SaveChangesAsync();

        var rows = context.Set<TB_AUDITLOG>().AsNoTracking().Where(l => l.ACTIONTYPE == AuditLogInterceptor.ActionUpdate).ToList();
        Assert.Equal(2, rows.Count);
        var nameRow = rows.Single(r => r.COLUMNNAME == "VAHEDNAME");
        Assert.Equal("واحد تست", nameRow.OLDVALUE);
        Assert.Equal("نام جدید", nameRow.NEWVALUE);
        Assert.Equal(vahed.ID, nameRow.RECORDID);
    }

    [Fact]
    public async Task No_actual_change_writes_nothing()
    {
        TB_VAHED_INFO vahed;
        await using (var seed = CreateContext())
        {
            vahed = Vahed();
            seed.Set<TB_VAHED_INFO>().Add(vahed);
            await seed.SaveChangesAsync();
        }

        await using var context = CreateContext();
        var tracked = await context.Set<TB_VAHED_INFO>().SingleAsync(v => v.ID == vahed.ID);
        tracked.VAHEDNAME = tracked.VAHEDNAME; // force EF to mark it modified without an actual value change
        context.Entry(tracked).Property(x => x.VAHEDNAME).IsModified = true;
        await context.SaveChangesAsync();

        Assert.Empty(context.Set<TB_AUDITLOG>().AsNoTracking().Where(l => l.ACTIONTYPE == AuditLogInterceptor.ActionUpdate));
    }

    [Fact]
    public async Task Hard_delete_writes_full_snapshot_in_oldvalue()
    {
        TB_VAHED_INFO vahed;
        await using (var seed = CreateContext())
        {
            vahed = Vahed();
            seed.Set<TB_VAHED_INFO>().Add(vahed);
            await seed.SaveChangesAsync();
        }

        await using var context = CreateContext();
        var tracked = await context.Set<TB_VAHED_INFO>().SingleAsync(v => v.ID == vahed.ID);
        context.Set<TB_VAHED_INFO>().Remove(tracked);
        await context.SaveChangesAsync();

        var log = context.Set<TB_AUDITLOG>().AsNoTracking().Single(l => l.ACTIONTYPE == AuditLogInterceptor.ActionDelete);
        Assert.Null(log.NEWVALUE);
        Assert.Contains("0042", log.OLDVALUE);
    }

    [Fact]
    public async Task Soft_delete_transition_logs_delete_not_a_column_update()
    {
        TB_VOUCHERSHEAD head;
        await using (var seed = CreateContext())
        {
            head = new TB_VOUCHERSHEAD { ID = Guid.NewGuid(), DOC_NUM = "000002", YEAR = "1404", VAHEDCODE = "0042", ISDELETED = false };
            seed.Set<TB_VOUCHERSHEAD>().Add(head);
            await seed.SaveChangesAsync();
        }

        await using var context = CreateContext();
        var tracked = await context.Set<TB_VOUCHERSHEAD>().SingleAsync(h => h.ID == head.ID);
        tracked.ISDELETED = true;
        await context.SaveChangesAsync();

        var log = context.Set<TB_AUDITLOG>().AsNoTracking().Single(l => l.TABLENAME == "TB_VOUCHERSHEAD" && l.ACTIONTYPE == AuditLogInterceptor.ActionDelete);
        Assert.Null(log.COLUMNNAME);
        Assert.Null(log.NEWVALUE);
        Assert.Contains("000002", log.OLDVALUE);
    }

    [Fact]
    public async Task Restoring_a_soft_deleted_row_logs_restore_with_current_snapshot()
    {
        TB_VOUCHERSHEAD head;
        await using (var seed = CreateContext())
        {
            head = new TB_VOUCHERSHEAD { ID = Guid.NewGuid(), DOC_NUM = "000003", YEAR = "1404", VAHEDCODE = "0042", ISDELETED = true };
            seed.Set<TB_VOUCHERSHEAD>().Add(head);
            await seed.SaveChangesAsync();
        }

        await using var context = CreateContext();
        var tracked = await context.Set<TB_VOUCHERSHEAD>().SingleAsync(h => h.ID == head.ID);
        tracked.ISDELETED = false;
        await context.SaveChangesAsync();

        var log = context.Set<TB_AUDITLOG>().AsNoTracking().Single(l => l.TABLENAME == "TB_VOUCHERSHEAD" && l.ACTIONTYPE == AuditLogInterceptor.ActionRestore);
        Assert.Null(log.OLDVALUE);
        Assert.Contains("000003", log.NEWVALUE);
    }

    [Fact]
    public async Task Label_uses_catalog_columns_for_voucher_head()
    {
        await using var context = CreateContext();
        var head = new TB_VOUCHERSHEAD { ID = Guid.NewGuid(), DOC_NUM = "000050", ATF_NUM = "AFT-1", YEAR = "1404", VAHEDCODE = "0042", ISDELETED = false };
        context.Set<TB_VOUCHERSHEAD>().Add(head);
        await context.SaveChangesAsync();

        var log = context.Set<TB_AUDITLOG>().AsNoTracking().Single();
        Assert.Equal("000050 - AFT-1", log.RECORDLABEL);
    }

    [Fact]
    public async Task Label_is_null_when_the_table_has_no_label_columns()
    {
        // LabelOf برای جدولی بدون LabelColumns همیشه null می‌دهد؛ fallback به keyText فقط برای کلیدهای
        // غیر-Guid-تکی به کار می‌آید (KeyOf یک Guid تکی را متنی نمی‌کند)، پس TB_VAHED_INFO برچسب ندارد.
        await using var context = CreateContext();
        context.Set<TB_VAHED_INFO>().Add(Vahed());
        await context.SaveChangesAsync();

        var log = context.Set<TB_AUDITLOG>().AsNoTracking().Single();
        Assert.Null(log.RECORDLABEL);
    }

    [Fact]
    public async Task Child_row_gets_parent_id_from_its_direct_parent()
    {
        TB_VOUCHERSHEAD head;
        await using (var seed = CreateContext())
        {
            head = new TB_VOUCHERSHEAD { ID = Guid.NewGuid(), DOC_NUM = "000004", YEAR = "1404", VAHEDCODE = "0042", ISDELETED = false };
            seed.Set<TB_VOUCHERSHEAD>().Add(head);
            await seed.SaveChangesAsync();
        }

        await using var context = CreateContext();
        var detail = new TB_VOUCHERSDETAIL { ID = Guid.NewGuid(), VOUCHERSHEAD_ID = head.ID, RADIF = 1, DEBTOR = 10, YEAR = "1404", VAHEDCODE = "0042", ISDELETED = false };
        context.Set<TB_VOUCHERSDETAIL>().Add(detail);
        await context.SaveChangesAsync();

        var log = context.Set<TB_AUDITLOG>().AsNoTracking().Single(l => l.TABLENAME == "TB_VOUCHERSDETAIL");
        Assert.Equal(head.ID, log.PARENTID);
    }

    [Fact]
    public async Task Grandchild_row_gets_the_topmost_ancestor_not_its_immediate_parent()
    {
        // ParentOfAsync keeps walking and overwrites `parent` at every hop, so a three-level chain
        // (ردیف تفصیلی ← ردیف سند ← سرسند) ends up pointing at سرسند, not at ردیف سند itself. این رفتار
        // فعلی تعمداً حفظ شده (گروه‌بندی زیر سرسند)؛ اگر PARENTID باید به والد بی‌واسطه اشاره کند، همین
        // تست باید رد شود و پیاده‌سازی تغییر کند.
        TB_VOUCHERSHEAD head;
        TB_VOUCHERSDETAIL detail;
        await using (var seed = CreateContext())
        {
            head = new TB_VOUCHERSHEAD { ID = Guid.NewGuid(), DOC_NUM = "000005", YEAR = "1404", VAHEDCODE = "0042", ISDELETED = false };
            detail = new TB_VOUCHERSDETAIL { ID = Guid.NewGuid(), VOUCHERSHEAD_ID = head.ID, RADIF = 1, YEAR = "1404", VAHEDCODE = "0042", ISDELETED = false };
            seed.Set<TB_VOUCHERSHEAD>().Add(head);
            seed.Set<TB_VOUCHERSDETAIL>().Add(detail);
            await seed.SaveChangesAsync();
        }

        await using var context = CreateContext();
        var link = new TB_VOUCHERDETAIL_LINK_TAFSILI
        {
            ID = Guid.NewGuid(), VOUCHERSDETAIL_ID = detail.ID, TAFSILI_ID = Guid.NewGuid(), LEVEL_ID = Guid.NewGuid(),
            ADDUSERID = "1234567890", CREATEDDATE = DateTime.UtcNow, ISDELETED = false,
        };
        context.Set<TB_VOUCHERDETAIL_LINK_TAFSILI>().Add(link);
        await context.SaveChangesAsync();

        var log = context.Set<TB_AUDITLOG>().AsNoTracking().Single(l => l.TABLENAME == "TB_VOUCHERDETAIL_LINK_TAFSILI");
        Assert.Equal(head.ID, log.PARENTID);
    }

    [Fact]
    public async Task Unauthenticated_save_is_attributed_to_system()
    {
        await using var context = CreateContext(authenticated: false);
        context.Set<TB_VAHED_INFO>().Add(Vahed());
        await context.SaveChangesAsync();

        var log = context.Set<TB_AUDITLOG>().AsNoTracking().Single();
        Assert.Equal("SYSTEM", log.USERID);
        Assert.Null(log.USERFULLNAME);
        // ستون VAHEDCODE ردیف خودش مقدار دارد ("0042")، پس با کاربر سیستمی (بدون واحد) جایگزین نمی‌شود —
        // fallback روی who.VahedCode فقط وقتی به کار می‌آید که ردیف خودش VAHEDCODE نداشته باشد.
        Assert.Equal("0042", log.VAHEDCODE);
    }

    [Fact]
    public async Task Vahedcode_defaults_to_the_caller_unit_when_the_row_has_none()
    {
        // TB_VAHED_INFO.VAHEDCODE کلید جانشین است (موجودیت‌های دیگر از آن FK می‌گیرند) و EF مقدار null
        // رویش را رهگیری نمی‌کند؛ برای آزمودن fallback باید جدولی با VAHEDCODE اختیاری به کار رود.
        await using var context = CreateContext();
        context.Set<TB_VOUCHERSHEAD>().Add(new TB_VOUCHERSHEAD
        {
            ID = Guid.NewGuid(), DOC_NUM = "000006", YEAR = "1404", VAHEDCODE = null, ISDELETED = false,
        });
        await context.SaveChangesAsync();

        var log = context.Set<TB_AUDITLOG>().AsNoTracking().Single();
        Assert.Equal("0042", log.VAHEDCODE);
    }

    [Fact]
    public async Task No_tracked_changes_writes_nothing()
    {
        await using var context = CreateContext();
        await context.SaveChangesAsync();
        Assert.Empty(context.Set<TB_AUDITLOG>().AsNoTracking());
    }

    [Fact]
    public async Task Bulk_label_collapses_the_whole_save_into_one_summary_row()
    {
        await using var context = CreateContext();
        context.BulkAuditLabel = "انتقال اسناد سال ۱۴۰۴ از سیستم قدیم";
        context.Set<TB_VOUCHERSHEAD>().Add(new TB_VOUCHERSHEAD { ID = Guid.NewGuid(), DOC_NUM = "000010", YEAR = "1404", VAHEDCODE = "0042", ISDELETED = false });
        context.Set<TB_VOUCHERSHEAD>().Add(new TB_VOUCHERSHEAD { ID = Guid.NewGuid(), DOC_NUM = "000011", YEAR = "1404", VAHEDCODE = "0042", ISDELETED = false });
        await context.SaveChangesAsync();

        var log = context.Set<TB_AUDITLOG>().AsNoTracking().Single();
        Assert.Equal("BULK", log.TABLENAME);
        Assert.Equal(AuditLogInterceptor.ActionInsert, log.ACTIONTYPE);
        Assert.Equal("انتقال اسناد سال ۱۴۰۴ از سیستم قدیم", log.RECORDLABEL);
        Assert.Contains("TB_VOUCHERSHEAD", log.NEWVALUE);
    }
}
