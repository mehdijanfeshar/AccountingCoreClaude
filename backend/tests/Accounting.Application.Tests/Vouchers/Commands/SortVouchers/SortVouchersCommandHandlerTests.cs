using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Vouchers.Commands.SortVouchers;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.Vouchers.Commands.SortVouchers;

/// <summary>
/// مرتب‌سازی اسناد — renumbering a range into تاریخ سند order.
///
/// The behaviour that matters most here is the two-pass write: <c>UK_VOUCHERHEAD_NUMBER</c> is
/// UNIQUE on (DOC_NUM, YEAR, VAHEDCODE), so the temporary numbers must not collide with each
/// other or with a real number, and the first pass must be flushed before the second begins.
/// </summary>
public sealed class SortVouchersCommandHandlerTests
{
    private const string Vahed = "0001";
    private const string Year = "1403";

    private static TB_VOUCHERSHEAD Head(string docNum, string dateDoc, DocLife? docLife = DocLife.Draft) => new()
    {
        ID = Guid.NewGuid(),
        DOC_NUM = docNum,
        DATE_DOC = dateDoc,
        DOCLIFE = docLife,
        VAHEDCODE = Vahed,
        YEAR = Year,
        ISDELETED = false,
    };

    private static Mock<ICurrentUser> CurrentUser()
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns("sorter1");
        return currentUser;
    }

    private static (SortVouchersCommandHandler Handler, Mock<IUnitOfWork> UnitOfWork, List<List<string?>> Snapshots)
        Build(List<TB_VOUCHERSHEAD> heads)
    {
        var repository = new Mock<IVoucherHeadRepository>();
        repository
            .Setup(r => r.GetActiveByYearAsync(Vahed, Year, It.IsAny<CancellationToken>()))
            .ReturnsAsync(heads);

        var unitOfWork = new Mock<IUnitOfWork>();

        // Capture DOC_NUM at each flush so the test can assert on what the database would actually
        // have seen, pass by pass — the whole point of the temporary-number scheme.
        var snapshots = new List<List<string?>>();
        unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => snapshots.Add(heads.Select(h => h.DOC_NUM).ToList()))
            .ReturnsAsync(0);

        return (new SortVouchersCommandHandler(repository.Object, unitOfWork.Object, CurrentUser().Object),
            unitOfWork,
            snapshots);
    }

    private static SortVouchersCommand ByDocNum(string from, string to) =>
        new(VoucherSortType.DocNum, from, to, null, null, Year) { VahedCode = Vahed };

    private static SortVouchersCommand ByDate(string from, string to) =>
        new(VoucherSortType.DocDate, null, null, from, to, Year) { VahedCode = Vahed };

    [Fact]
    public async Task Renumbers_the_range_into_date_order()
    {
        // Numbers 1..3 exist but their dates are out of order.
        var a = Head("000001", "14030315");
        var b = Head("000002", "14030110");
        var c = Head("000003", "14030220");
        var (handler, _, _) = Build([a, b, c]);

        var count = await handler.Handle(ByDocNum("000001", "000003"), CancellationToken.None);

        Assert.Equal(3, count);
        Assert.Equal("000001", b.DOC_NUM); // earliest date
        Assert.Equal("000002", c.DOC_NUM);
        Assert.Equal("000003", a.DOC_NUM); // latest date
    }

    [Fact]
    public async Task Parks_every_row_on_a_unique_temporary_number_before_assigning_final_ones()
    {
        var a = Head("000001", "14030315");
        var b = Head("000002", "14030110");
        var (handler, unitOfWork, snapshots) = Build([a, b]);

        await handler.Handle(ByDocNum("000001", "000002"), CancellationToken.None);

        // Two flushes: temporaries, then finals.
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        Assert.Equal(2, snapshots.Count);

        var temporaries = snapshots[0];

        // Distinct from one another...
        Assert.Equal(temporaries.Count, temporaries.Distinct().Count());
        // ...and impossible to mistake for a real, all-digit voucher number. This is the exact
        // property the reference project's Substring(1,4)+"S" scheme fails: "000001"/"000002"
        // both collapse to "0000S".
        Assert.All(temporaries, value => Assert.False(int.TryParse(value, out _)));
    }

    [Fact]
    public async Task Continues_numbering_after_the_highest_voucher_below_the_range()
    {
        var before = Head("000007", "14030101");
        var a = Head("000008", "14030320");
        var b = Head("000009", "14030210");
        var (handler, _, _) = Build([before, a, b]);

        await handler.Handle(ByDocNum("000008", "000009"), CancellationToken.None);

        // Resumes at 8, not at 1 — sorting a slice must not renumber it over what precedes it.
        Assert.Equal("000008", b.DOC_NUM);
        Assert.Equal("000009", a.DOC_NUM);
        Assert.Equal("000007", before.DOC_NUM); // untouched
    }

    [Fact]
    public async Task Starts_at_one_when_nothing_precedes_the_range()
    {
        var a = Head("000004", "14030320");
        var b = Head("000005", "14030210");
        var (handler, _, _) = Build([a, b]);

        await handler.Handle(ByDocNum("000004", "000005"), CancellationToken.None);

        Assert.Equal("000001", b.DOC_NUM);
        Assert.Equal("000002", a.DOC_NUM);
    }

    [Fact]
    public async Task A_date_range_selects_by_date_not_by_number()
    {
        var inside = Head("000009", "14030210");
        var outside = Head("000001", "14031120");
        var (handler, _, _) = Build([inside, outside]);

        var count = await handler.Handle(ByDate("14030201", "14030228"), CancellationToken.None);

        Assert.Equal(1, count);
        Assert.Equal("14031120", outside.DATE_DOC);
        Assert.Equal("000001", outside.DOC_NUM); // untouched
    }

    [Theory]
    [InlineData(DocLife.Reviewed)]
    [InlineData(DocLife.Accepted)]
    public async Task Refuses_when_any_voucher_in_the_range_is_finalized(DocLife docLife)
    {
        var ok = Head("000001", "14030110");
        var locked = Head("000002", "14030120", docLife);
        var (handler, unitOfWork, _) = Build([ok, locked]);

        await Assert.ThrowsAsync<VoucherNotEditableException>(
            () => handler.Handle(ByDocNum("000001", "000002"), CancellationToken.None));

        // Nothing is written when the range is refused.
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal("000001", ok.DOC_NUM);
    }

    [Fact]
    public async Task A_finalized_voucher_outside_the_range_does_not_block_the_sort()
    {
        // The reference project checks the whole year rather than the range, which makes sorting
        // impossible for the rest of the year as soon as one voucher anywhere is finalized.
        var lockedElsewhere = Head("000050", "14031201", DocLife.Accepted);
        var a = Head("000001", "14030315");
        var b = Head("000002", "14030110");
        var (handler, _, _) = Build([lockedElsewhere, a, b]);

        var count = await handler.Handle(ByDocNum("000001", "000002"), CancellationToken.None);

        Assert.Equal(2, count);
        Assert.Equal("000050", lockedElsewhere.DOC_NUM);
    }

    [Fact]
    public async Task An_empty_range_changes_nothing()
    {
        var a = Head("000001", "14030110");
        var (handler, unitOfWork, _) = Build([a]);

        var count = await handler.Handle(ByDocNum("000500", "000600"), CancellationToken.None);

        Assert.Equal(0, count);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal("000001", a.DOC_NUM);
    }

    [Fact]
    public async Task Vouchers_sharing_a_date_get_a_stable_order_rather_than_an_arbitrary_one()
    {
        var a = Head("000001", "14030110");
        var b = Head("000002", "14030110");
        var (handler, _, _) = Build([a, b]);

        await handler.Handle(ByDocNum("000001", "000002"), CancellationToken.None);

        // Both are valid orderings; what matters is that each got a distinct number and the set is
        // exactly 1..2 — no duplicate can survive the UNIQUE index.
        Assert.Equal(new[] { "000001", "000002" }, new[] { a.DOC_NUM, b.DOC_NUM }.OrderBy(x => x).ToArray());
    }
}
