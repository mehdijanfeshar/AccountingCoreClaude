using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Tests.Legacy;

/// <summary>
/// شمارهٔ عطف سند: سال + واحد + ردیف ۷ رقمی، ادامه از بزرگ‌ترین عطف موجود (هر دو قالب)، و تغییرناپذیر.
/// </summary>
public sealed class VoucherAtfNumberInterceptorTests : IDisposable
{
    private const string CreateTableSql = """
        CREATE TABLE TB_VOUCHERSHEAD (
            ID TEXT PRIMARY KEY, DOC_NUM TEXT, DATE_DOC TEXT, DOCLIFE INTEGER,
            HEAD_DESC TEXT, APENDIX TEXT, SYSTEM_TYPE TEXT, FLAG_STATE TEXT,
            CREATEDDATE TEXT, UPDATEDDATE TEXT, ADDUSERID TEXT, CHANGEUSERID TEXT,
            VAHEDCODE TEXT, YEAR TEXT, ISDELETED INTEGER, ATTACHFILE BLOB,
            ATTACHFILE_NAME TEXT, ATF_NUM TEXT, ISAUTOMATIC INTEGER,
            SNDVAHEDCODE TEXT, PARENTHEAD_ID TEXT, GLOBALNUMBER TEXT
        );
        """;

    private readonly SqliteConnection _connection;

    public VoucherAtfNumberInterceptorTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        using var setup = CreateContext();
        setup.Database.ExecuteSqlRaw(CreateTableSql);
    }

    public void Dispose() => _connection.Dispose();

    private LegacyDbContext CreateContext() => new(
        new DbContextOptionsBuilder<LegacyDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new VoucherAtfNumberInterceptor())
            .Options);

    private static TB_VOUCHERSHEAD Head(string year = "1404", string vahed = "1155", string? atf = null, string docNum = "000001") => new()
    {
        ID = Guid.NewGuid(),
        DOC_NUM = docNum,
        DATE_DOC = year + "0101",
        YEAR = year,
        VAHEDCODE = vahed,
        ATF_NUM = atf,
        ISDELETED = false,
    };

    [Fact]
    public async Task First_voucher_of_a_unit_and_year_gets_sequence_one_in_year_unit_format()
    {
        await using var context = CreateContext();
        var head = Head();
        context.TB_VOUCHERSHEADs.Add(head);
        await context.SaveChangesAsync();

        Assert.Equal("140411550000001", head.ATF_NUM);
    }

    [Fact]
    public async Task Continues_from_the_largest_existing_number_in_either_format_including_deleted()
    {
        await using (var seed = CreateContext())
        {
            // Inserted through raw SQL so the interceptor does not renumber the seed rows.
            seed.Database.ExecuteSqlRaw(
                "INSERT INTO TB_VOUCHERSHEAD (ID, YEAR, VAHEDCODE, ATF_NUM, ISDELETED) VALUES " +
                "({0}, '1404', '1155', '115514040000050', 1), ({1}, '1404', '1155', '140411550000012', 0), " +
                "({2}, '1405', '1155', '140511550000099', 0), ({3}, '1404', '0000', '140400000000077', 0)",
                Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
        }

        await using var context = CreateContext();
        var first = Head(docNum: "000060");
        var second = Head(docNum: "000061");
        context.TB_VOUCHERSHEADs.AddRange(first, second);
        await context.SaveChangesAsync();

        // Legacy 115514040000050 (deleted) is the max for 1404/1155; other years and units do not count.
        Assert.Equal("140411550000051", first.ATF_NUM);
        Assert.Equal("140411550000052", second.ATF_NUM);
    }

    [Fact]
    public async Task A_client_supplied_number_on_create_is_replaced()
    {
        await using var context = CreateContext();
        var head = Head(atf: "999999999999999");
        context.TB_VOUCHERSHEADs.Add(head);
        await context.SaveChangesAsync();

        Assert.Equal("140411550000001", head.ATF_NUM);
    }

    [Fact]
    public async Task The_number_never_changes_after_creation_even_when_the_voucher_is_renumbered()
    {
        var id = Guid.Empty;
        await using (var create = CreateContext())
        {
            var head = Head();
            create.TB_VOUCHERSHEADs.Add(head);
            await create.SaveChangesAsync();
            id = head.ID;
        }

        await using (var edit = CreateContext())
        {
            var head = await edit.TB_VOUCHERSHEADs.SingleAsync(h => h.ID == id);
            head.DOC_NUM = "000099";          // «مرتب‌سازی» renumbers the voucher…
            head.ATF_NUM = "140411550000777"; // …and nothing may touch the reference number.
            await edit.SaveChangesAsync();
        }

        await using var read = CreateContext();
        var saved = await read.TB_VOUCHERSHEADs.AsNoTracking().SingleAsync(h => h.ID == id);
        Assert.Equal("000099", saved.DOC_NUM);
        Assert.Equal("140411550000001", saved.ATF_NUM);
    }
}
