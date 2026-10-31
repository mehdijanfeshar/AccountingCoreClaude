using Accounting.Application.Accrual;
using Accounting.Application.Commands.Vouchers.ReverseVoucher;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.OperationTemplates;
using Accounting.Domain.Accrual;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.Accrual;

/// <summary>اجرای یک سطر زمان‌بندی (فاز ۶۶). قفل اوراکل اینجا دیده نمی‌شود (ریسک #۲۵) — فقط منطق.</summary>
public sealed class AccrualScheduleRunnerTests
{
    private const string Vahed = "0001";

    private readonly Mock<IAccrualScheduleRepository> _repo = new();
    private readonly Mock<IVoucherHeadRepository> _heads = new();
    private readonly Mock<IVoucherDetailRepository> _details = new();
    private readonly Mock<IVoucherWriter> _writer = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly List<TB_VOUCHERSHEAD> _addedHeads = [];
    private readonly TB_VOUCHERSHEAD _source = new()
    {
        ID = Guid.NewGuid(), DOC_NUM = "000042", DATE_DOC = "14050720", DOCLIFE = DocLife.Accepted,
        VAHEDCODE = Vahed, YEAR = "1405", ISAUTOMATIC = true, ISDELETED = false,
    };

    public AccrualScheduleRunnerTests()
    {
        _user.SetupGet(u => u.UserId).Returns("u1");
        _heads.Setup(r => r.GetForUpdateAsync(_source.ID, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(_source);
        _heads.Setup(r => r.GetNextDocNumAsync(Vahed, It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync("000777");
        _heads.Setup(r => r.AddAsync(It.IsAny<TB_VOUCHERSHEAD>(), It.IsAny<CancellationToken>()))
            .Callback<TB_VOUCHERSHEAD, CancellationToken>((h, _) => _addedHeads.Add(h));
        _details.Setup(r => r.GetActiveTafsiliLinksAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        SourceLines(Line(80_000_000, 0), Line(0, 80_000_000));
    }

    private void SourceLines(params TB_VOUCHERSDETAIL[] lines) =>
        _details.Setup(r => r.GetActiveByHeadAsync(_source.ID, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(lines.ToList());

    private static TB_VOUCHERSDETAIL Line(decimal debtor, decimal creditor, Guid? checkId = null) => new()
    {
        ID = Guid.NewGuid(), ACCOUNT_ID = Guid.NewGuid(), DEBTOR = debtor, CREDITOR = creditor, CHECK_ID = checkId,
        VAHEDCODE = Vahed, YEAR = "1405", ISDELETED = false,
    };

    private AccrualSchedule Row(AccrualScheduleKind kind = AccrualScheduleKind.Reversal, AccrualScheduleState state = AccrualScheduleState.Open)
    {
        var row = new AccrualSchedule
        {
            Id = Guid.NewGuid(), VahedCode = Vahed, Year = "1405", SourceVoucherId = _source.ID, Kind = kind, LineNo = 1,
            DueDate = "14050801", Amount = 80_000_000, Description = "برگشت خودکار برآورد: نظافت مهر", State = state,
            DebitAccountId = kind == AccrualScheduleKind.Reversal ? null : Guid.NewGuid(),
            CreditAccountId = kind == AccrualScheduleKind.Reversal ? null : Guid.NewGuid(),
        };
        _repo.Setup(r => r.LockAsync(row.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(row);
        return row;
    }

    private AccrualScheduleRunner Runner() => new(_repo.Object,
        new VoucherReverser(_heads.Object, _details.Object, _user.Object), _writer.Object, _uow.Object, _user.Object);

    [Fact]
    public async Task Reversal_is_temporary_dated_due_date_and_linked_to_source()
    {
        var row = Row();

        var r = await Runner().RunAsync(row.Id, Vahed, skipLocked: false, CancellationToken.None);

        var head = Assert.Single(_addedHeads);
        Assert.Equal(AccrualRunOutcome.Ran, r.Outcome);
        Assert.Equal(head.ID, r.VoucherId);
        Assert.Equal(_source.ID, head.PARENTHEAD_ID);
        Assert.Equal("14050801", head.DATE_DOC);
        Assert.Equal("1405", head.YEAR);
        Assert.Equal(DocLife.Temporary, head.DOCLIFE);
        Assert.True(head.ISAUTOMATIC);
        Assert.Equal(AccrualScheduleState.Done, row.State);
        Assert.Equal(head.ID, row.ResultVoucherId);
        Assert.Equal("u1", row.ChangedBy);
        _uow.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Running_a_done_line_again_returns_the_same_voucher_and_builds_nothing()
    {
        var row = Row(state: AccrualScheduleState.Done);
        var previous = Guid.NewGuid();
        row.ResultVoucherId = previous;

        var r = await Runner().RunAsync(row.Id, Vahed, skipLocked: false, CancellationToken.None);

        Assert.Equal(AccrualRunOutcome.AlreadyDone, r.Outcome);
        Assert.Equal(previous, r.VoucherId);
        Assert.Empty(_addedHeads);
        _writer.Verify(w => w.CreateDraftVoucherAsync(It.IsAny<VoucherDraft>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cancelled_line_is_not_run()
    {
        var row = Row(state: AccrualScheduleState.Cancelled);

        await Assert.ThrowsAsync<BusinessRuleException>(() => Runner().RunAsync(row.Id, Vahed, false, CancellationToken.None));

        Assert.Empty(_addedHeads);
        _uow.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Line_of_another_unit_is_not_found()
    {
        var row = Row();

        await Assert.ThrowsAsync<NotFoundException>(() => Runner().RunAsync(row.Id, "0002", false, CancellationToken.None));
        Assert.Equal(AccrualScheduleState.Open, row.State);
    }

    [Fact]
    public async Task Line_locked_elsewhere_is_skipped_by_the_job()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.LockAsync(id, true, It.IsAny<CancellationToken>())).ReturnsAsync((AccrualSchedule?)null);

        var r = await Runner().RunAsync(id, vahedCode: null, skipLocked: true, CancellationToken.None);

        Assert.Equal(AccrualRunOutcome.Skipped, r.Outcome);
    }

    [Fact]
    public async Task Unbalanced_reversal_is_refused_and_line_stays_open()
    {
        // ردیف چک کپی نمی‌شود ⇒ برگشت نامتراز.
        SourceLines(Line(80_000_000, 0), Line(0, 80_000_000, checkId: Guid.NewGuid()));
        var row = Row();

        await Assert.ThrowsAsync<BusinessRuleException>(() => Runner().RunAsync(row.Id, Vahed, false, CancellationToken.None));

        Assert.Equal(AccrualScheduleState.Open, row.State);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Prepaid_share_is_written_through_the_voucher_writer_on_due_date()
    {
        var row = Row(AccrualScheduleKind.Prepaid);
        row.Amount = 10_000_000;
        row.DueDate = "14050830";
        row.TafsiliJson = """{"debit":[{"level":1,"levelId":"11111111-1111-1111-1111-111111111111","detailId":"22222222-2222-2222-2222-222222222222","title":"مرکز ۴۵۰۱"}],"credit":[]}""";
        VoucherDraft? written = null;
        var created = Guid.NewGuid();
        _writer.Setup(w => w.CreateDraftVoucherAsync(It.IsAny<VoucherDraft>(), It.IsAny<CancellationToken>()))
            .Callback<VoucherDraft, CancellationToken>((d, _) => written = d)
            .ReturnsAsync((created, "000123"));

        var r = await Runner().RunAsync(row.Id, Vahed, false, CancellationToken.None);

        Assert.Equal(created, r.VoucherId);
        Assert.Equal("000123", r.VoucherNo);
        Assert.NotNull(written);
        Assert.Equal("14050830", AccrualDates.ToJalali(written!.VoucherDate));
        Assert.Equal(10_000_000, written.TotalDebit);
        Assert.Equal(written.TotalDebit, written.TotalCredit);
        Assert.Equal(row.DebitAccountId, written.Lines[0].SubsidiaryAccountId);
        Assert.Equal(Guid.Parse("22222222-2222-2222-2222-222222222222"), Assert.Single(written.Lines[0].Details).DetailId);
        Assert.Equal(AccrualScheduleState.Done, row.State);
        Assert.Empty(_addedHeads);
    }
}
