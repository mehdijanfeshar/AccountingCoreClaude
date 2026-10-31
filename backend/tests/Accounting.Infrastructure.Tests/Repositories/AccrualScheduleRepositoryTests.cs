using Accounting.Application.Accrual;
using Accounting.Domain.Accrual;
using Accounting.Infrastructure.Accrual;
using Accounting.Infrastructure.Legacy;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Tests.Repositories;

/// <summary>ترجمهٔ کوئری‌های زمان‌بندی تعهدی (DDL 090) روی SQLite. قفل اوراکل اینجا دیده نمی‌شود (ریسک #۲۵).</summary>
public sealed class AccrualScheduleRepositoryTests : IDisposable
{
    private const string CreateTableSql = """
        CREATE TABLE TB_AC_SCHEDULE (
            ID TEXT PRIMARY KEY, VAHEDCODE TEXT NOT NULL, YEAR TEXT NOT NULL, SOURCE_VOUCHER_ID TEXT NOT NULL,
            EXECUTION_ID TEXT, KIND INTEGER NOT NULL, LINE_NO INTEGER NOT NULL, DUE_DATE TEXT NOT NULL,
            AMOUNT INTEGER NOT NULL, DEBIT_ACCOUNT_ID TEXT, CREDIT_ACCOUNT_ID TEXT, TAFSILI_JSON TEXT,
            DESCRIPTION TEXT, STATE INTEGER NOT NULL, RESULT_VOUCHER_ID TEXT, ISDELETED INTEGER NOT NULL,
            ADDUSERID TEXT NOT NULL, CREATEDDATE TEXT NOT NULL, CHANGEUSERID TEXT, UPDATEDDATE TEXT,
            UNIQUE (SOURCE_VOUCHER_ID, KIND, LINE_NO)
        );
        """;

    private readonly SqliteConnection _connection;

    public AccrualScheduleRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        using var setup = CreateContext();
        setup.Database.ExecuteSqlRaw(CreateTableSql);
    }

    public void Dispose() => _connection.Dispose();

    private LegacyDbContext CreateContext() => new(new DbContextOptionsBuilder<LegacyDbContext>().UseSqlite(_connection).Options);

    private static AccrualSchedule Row(string vahed, AccrualScheduleKind kind, string due, AccrualScheduleState state = AccrualScheduleState.Open,
        bool deleted = false, Guid? source = null, int lineNo = 1) => new()
    {
        Id = Guid.NewGuid(), VahedCode = vahed, Year = due[..4], SourceVoucherId = source ?? Guid.NewGuid(), Kind = kind, LineNo = lineNo,
        DueDate = due, Amount = 1_000, State = state, IsDeleted = deleted, CreatedBy = "u1", CreatedAtUtc = DateTime.UtcNow,
    };

    private async Task SeedAsync(params AccrualSchedule[] rows)
    {
        await using var db = CreateContext();
        await new AccrualScheduleRepository(db).AddRangeAsync(rows, default);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Due_ids_are_open_live_rows_up_to_today_oldest_first()
    {
        var old = Row("0001", AccrualScheduleKind.Reversal, "14050701");
        var today = Row("0002", AccrualScheduleKind.Prepaid, "14050801");
        await SeedAsync(today, old,
            Row("0001", AccrualScheduleKind.Prepaid, "14050802"),
            Row("0001", AccrualScheduleKind.Reversal, "14050701", AccrualScheduleState.Done),
            Row("0001", AccrualScheduleKind.Reversal, "14050701", AccrualScheduleState.Cancelled),
            Row("0001", AccrualScheduleKind.Reversal, "14050701", deleted: true));

        await using var db = CreateContext();
        var ids = await new AccrualScheduleRepository(db).ListDueIdsAsync("14050801", 10, default);

        Assert.Equal([old.Id, today.Id], ids);
    }

    [Fact]
    public async Task Open_due_counts_per_unit_split_reversals_and_shares()
    {
        await SeedAsync(
            Row("0001", AccrualScheduleKind.Reversal, "14050301"),
            Row("0001", AccrualScheduleKind.Reversal, "14040101"),
            Row("0001", AccrualScheduleKind.Prepaid, "14050331"),
            Row("0001", AccrualScheduleKind.Deferred, "14050401"),
            Row("0002", AccrualScheduleKind.Deferred, "14050315", AccrualScheduleState.Done));

        await using var db = CreateContext();
        var counts = await new AccrualScheduleRepository(db).CountOpenDueAsync("14050331", default);

        var unit = Assert.Single(counts);
        Assert.Equal("0001", unit.Key);
        Assert.Equal(new AccrualOpenCount(2, 1), unit.Value);
    }

    [Fact]
    public async Task List_filters_by_unit_state_kind_and_due_range()
    {
        var match = Row("0001", AccrualScheduleKind.Prepaid, "14050730");
        await SeedAsync(match,
            Row("0002", AccrualScheduleKind.Prepaid, "14050730"),
            Row("0001", AccrualScheduleKind.Reversal, "14050730"),
            Row("0001", AccrualScheduleKind.Prepaid, "14050830"),
            Row("0001", AccrualScheduleKind.Prepaid, "14050730", AccrualScheduleState.Done));

        await using var db = CreateContext();
        var rows = await new AccrualScheduleRepository(db).ListAsync("0001",
            new AccrualScheduleFilter(AccrualScheduleState.Open, AccrualScheduleKind.Prepaid, "14050701", "14050731"), default);

        Assert.Equal(match.Id, Assert.Single(rows).Id);
    }

    [Fact]
    public async Task Lock_outside_oracle_returns_the_tracked_row()
    {
        var row = Row("0001", AccrualScheduleKind.Reversal, "14050801");
        await SeedAsync(row);

        await using var db = CreateContext();
        var locked = await new AccrualScheduleRepository(db).LockAsync(row.Id, skipLocked: true, default);

        Assert.NotNull(locked);
        Assert.Equal(EntityState.Unchanged, db.Entry(locked!).State);
    }
}
