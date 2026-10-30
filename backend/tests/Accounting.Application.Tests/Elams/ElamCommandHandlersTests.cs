using Accounting.Application.Commands.Vouchers.Common;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Elams;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.Elams;

/// <summary>
/// گذارهای وضعیت اعلامیه (فاز ۴۶): ثبت ⇐ صدور سند ⇐ تأیید اولیه ⇐ تأیید نهایی، قفل ویرایش/حذف
/// پس از سند، و ارسال درآمد به سبا.
/// </summary>
public class ElamCommandHandlersTests
{
    private readonly Mock<IElamWorkflowRepository> _repo = new();
    private readonly Mock<IVoucherHeadRepository> _heads = new();
    private readonly Mock<IVoucherDetailRepository> _details = new();
    private readonly Mock<IVoucherTafsiliLevelGuard> _guard = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Mock<IRevenueElamSender> _sender = new();
    private readonly List<TB_ELAMHEAD> _addedHeads = new();

    public ElamCommandHandlersTests()
    {
        _user.SetupGet(u => u.UserId).Returns("1234567890");
        _repo.Setup(r => r.GetRabetAccountAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string code, CancellationToken _) => new ElamRabetAccount(Guid.NewGuid(), "90000" + code));
        _repo.Setup(r => r.GetNextSerialAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("00421404000001");
        _repo.Setup(r => r.GetVahedNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync("واحد");
        _repo.Setup(r => r.AddHeadAsync(It.IsAny<TB_ELAMHEAD>(), It.IsAny<CancellationToken>()))
            .Callback<TB_ELAMHEAD, CancellationToken>((h, _) => _addedHeads.Add(h));
        _heads.Setup(h => h.GetNextDocNumAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("000001");
    }

    private ElamCommandHandlers Handlers()
        => new(_repo.Object,
            new ElamWorkflowService(_repo.Object, _heads.Object, _details.Object, _guard.Object, _user.Object),
            _uow.Object, _user.Object, _sender.Object);

    private TB_ELAMHEAD Stored(ElamWebStat stat, bool withVoucher = false)
    {
        var head = new TB_ELAMHEAD
        {
            ID = Guid.NewGuid(),
            WEB_STAT = (byte)stat,
            ELAMH_CASE = ElamCase.Debtor,
            ELAMH_DATE = "14040510",
            ELAMH_SERIALNO = "00421404000001",
            ELAMH_SENDRCVVAHED = "0051",
            VAHEDCODE = "0042",
            YEAR = "1404",
            VOUCHERSHEAD_ID = withVoucher ? Guid.NewGuid() : null,
            ISDELETED = false,
        };
        head.TB_ELAMDETAILs.Add(new TB_ELAMDETAIL { ID = Guid.NewGuid(), ACCOUNTCODE_ID = Guid.NewGuid(), DEBTOR = 0, CREDITOR = 100, ISDELETED = false });
        _repo.Setup(r => r.GetWithDetailsForUpdateAsync(head.ID, "0042", It.IsAny<CancellationToken>())).ReturnsAsync(head);
        return head;
    }

    private static CreateOtherElamCommand OtherCommand(string counter = "0051") => new(
        "1404", "14040510", " شرح ", ElamCase.Debtor, counter, null, " ",
        new[] { new ElamDetailInput(Guid.NewGuid(), 100m, null, null, null) }) { VahedCode = "0042" };

    [Fact]
    public async Task CreateOther_stages_head_in_create_state_and_saves_once()
    {
        var id = await Handlers().Handle(OtherCommand(), default);

        var head = Assert.Single(_addedHeads);
        Assert.Equal(id, head.ID);
        Assert.Equal((byte)ElamWebStat.CreateOther, head.WEB_STAT);
        Assert.Equal("00421404000001", head.ELAMH_SERIALNO);
        Assert.Equal("900001", head.ELAMH_CODE);
        Assert.Equal("شرح", head.ELAMH_DESC);
        Assert.Null(head.ELAMH_DABIRDATE);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateOther_to_own_unit_conflicts()
        => await Assert.ThrowsAsync<ElamConflictException>(() => Handlers().Handle(OtherCommand("0042"), default));

    [Fact]
    public async Task CreateOther_to_unknown_unit_conflicts()
    {
        _repo.Setup(r => r.GetVahedNameAsync("9999", It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        await Assert.ThrowsAsync<ElamConflictException>(() => Handlers().Handle(OtherCommand("9999"), default));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateRevenue_is_always_creditor_with_debtor_line()
    {
        var cmd = new CreateRevenueElamCommand("1404", "14040510", "درآمد", DaramElamhType.PersonalDramadElam, "0051",
            Guid.NewGuid(), 700m, null, null, "1234567890", "کارگاه", "1", null, "05", null, null, null) { VahedCode = "0042" };

        await Handlers().Handle(cmd, default);

        var head = Assert.Single(_addedHeads);
        Assert.Equal(ElamCase.Creditor, head.ELAMH_CASE);
        Assert.Equal((byte)ElamWebStat.CreateDramad, head.WEB_STAT);
        _repo.Verify(r => r.AddDetailAsync(It.Is<TB_ELAMDETAIL>(d => d.DEBTOR == 700m && d.CREDITOR == 0m), It.IsAny<CancellationToken>()));
        _repo.Verify(r => r.GetRabetAccountAsync("3", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task UpdateOther_after_voucher_conflicts()
    {
        var head = Stored(ElamWebStat.VoucherOther, withVoucher: true);
        var cmd = new UpdateOtherElamCommand(head.ID, "1404", "14040510", "x", ElamCase.Debtor, "0051", null, null,
            new[] { new ElamDetailInput(Guid.NewGuid(), 1m, null, null, null) }) { VahedCode = "0042" };

        await Assert.ThrowsAsync<ElamConflictException>(() => Handlers().Handle(cmd, default));
    }

    [Fact]
    public async Task UpdateOther_on_revenue_elam_conflicts()
    {
        var head = Stored(ElamWebStat.CreateDramad);
        var cmd = new UpdateOtherElamCommand(head.ID, "1404", "14040510", "x", ElamCase.Debtor, "0051", null, null,
            new[] { new ElamDetailInput(Guid.NewGuid(), 1m, null, null, null) }) { VahedCode = "0042" };

        await Assert.ThrowsAsync<ElamConflictException>(() => Handlers().Handle(cmd, default));
    }

    [Fact]
    public async Task UpdateOther_replaces_details()
    {
        var head = Stored(ElamWebStat.CreateOther);
        var old = head.TB_ELAMDETAILs.Single();
        var cmd = new UpdateOtherElamCommand(head.ID, "1404", "14040511", "جدید", ElamCase.Creditor, "0051", null, null,
            new[] { new ElamDetailInput(Guid.NewGuid(), 9m, null, null, null) }) { VahedCode = "0042" };

        await Handlers().Handle(cmd, default);

        Assert.True(old.ISDELETED);
        Assert.Equal("14040511", head.ELAMH_DATE);
        Assert.Equal(ElamCase.Creditor, head.ELAMH_CASE);
        _repo.Verify(r => r.AddDetailAsync(It.Is<TB_ELAMDETAIL>(d => d.DEBTOR == 9m), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task Delete_missing_is_not_found()
        => await Assert.ThrowsAsync<NotFoundException>(() =>
            Handlers().Handle(new DeleteElamCommand(Guid.NewGuid()) { VahedCode = "0042" }, default));

    [Fact]
    public async Task Delete_after_voucher_conflicts()
    {
        var head = Stored(ElamWebStat.VoucherOther, withVoucher: true);
        await Assert.ThrowsAsync<ElamConflictException>(() =>
            Handlers().Handle(new DeleteElamCommand(head.ID) { VahedCode = "0042" }, default));
        Assert.NotEqual(true, head.ISDELETED);
    }

    [Fact]
    public async Task Delete_soft_deletes_head_and_details()
    {
        var head = Stored(ElamWebStat.CreateOther);
        await Handlers().Handle(new DeleteElamCommand(head.ID) { VahedCode = "0042" }, default);
        Assert.True(head.ISDELETED);
        Assert.True(head.TB_ELAMDETAILs.Single().ISDELETED);
    }

    [Theory]
    [InlineData(ElamWebStat.CreateOther, ElamWebStat.VoucherOther)]
    [InlineData(ElamWebStat.CreateDramad, ElamWebStat.VoucherDramad)]
    [InlineData(ElamWebStat.RcvInfo, ElamWebStat.ConfirmRcvInfo)]
    public async Task IssueVoucher_moves_to_voucher_state(ElamWebStat from, ElamWebStat to)
    {
        var head = Stored(from);
        var result = await Handlers().Handle(new IssueElamVoucherCommand(head.ID) { VahedCode = "0042" }, default);

        Assert.Equal((byte)to, head.WEB_STAT);
        Assert.Equal(head.VOUCHERSHEAD_ID, result.VoucherHeadId);
        Assert.Equal("000001", result.DocNum);
    }

    [Fact]
    public async Task IssueVoucher_twice_conflicts()
    {
        var head = Stored(ElamWebStat.VoucherOther, withVoucher: true);
        await Assert.ThrowsAsync<ElamConflictException>(() =>
            Handlers().Handle(new IssueElamVoucherCommand(head.ID) { VahedCode = "0042" }, default));
    }

    [Fact]
    public async Task IssueVoucher_in_confirm_state_without_voucher_conflicts()
    {
        var head = Stored(ElamWebStat.FirstConfirmOther);
        await Assert.ThrowsAsync<ElamConflictException>(() =>
            Handlers().Handle(new IssueElamVoucherCommand(head.ID) { VahedCode = "0042" }, default));
    }

    [Fact]
    public async Task Confirm_without_voucher_conflicts()
    {
        var head = Stored(ElamWebStat.CreateOther);
        await Assert.ThrowsAsync<ElamConflictException>(() =>
            Handlers().Handle(new ConfirmElamCommand(head.ID, false) { VahedCode = "0042" }, default));
    }

    [Fact]
    public async Task Confirm_received_conflicts()
    {
        var head = Stored(ElamWebStat.ConfirmRcvInfo, withVoucher: true);
        await Assert.ThrowsAsync<ElamConflictException>(() =>
            Handlers().Handle(new ConfirmElamCommand(head.ID, false) { VahedCode = "0042" }, default));
    }

    [Fact]
    public async Task First_confirm_moves_voucher_state_forward()
    {
        var head = Stored(ElamWebStat.VoucherOther, withVoucher: true);
        var result = await Handlers().Handle(new ConfirmElamCommand(head.ID, false) { VahedCode = "0042" }, default);
        Assert.Null(result);
        Assert.Equal((byte)ElamWebStat.FirstConfirmOther, head.WEB_STAT);
    }

    [Fact]
    public async Task Final_confirm_before_first_conflicts()
    {
        var head = Stored(ElamWebStat.VoucherOther, withVoucher: true);
        await Assert.ThrowsAsync<ElamConflictException>(() =>
            Handlers().Handle(new ConfirmElamCommand(head.ID, true) { VahedCode = "0042" }, default));
    }

    [Fact]
    public async Task Final_confirm_twice_conflicts()
    {
        var head = Stored(ElamWebStat.FinalConfirmOther, withVoucher: true);
        var ex = await Assert.ThrowsAsync<ElamConflictException>(() =>
            Handlers().Handle(new ConfirmElamCommand(head.ID, true) { VahedCode = "0042" }, default));
        Assert.Contains("قبلاً ارسال", ex.PublicDetail);
    }

    [Fact]
    public async Task Final_confirm_sent_creates_received_elam_in_target_unit()
    {
        var head = Stored(ElamWebStat.FirstConfirmOther, withVoucher: true);

        var rcvId = await Handlers().Handle(new ConfirmElamCommand(head.ID, true) { VahedCode = "0042" }, default);

        Assert.Equal((byte)ElamWebStat.FinalConfirmOther, head.WEB_STAT);
        var rcv = Assert.Single(_addedHeads);
        Assert.Equal(rcv.ID, rcvId);
        Assert.Equal("0051", rcv.VAHEDCODE);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private TB_ELAMHEAD ReadyRevenue()
    {
        var head = Stored(ElamWebStat.FirstConfirmDramad, withVoucher: true);
        head.ELAMH_CASE = ElamCase.Creditor;
        head.ELAMH_WORKSHOPCODE = "1234567890";
        head.ELAMH_WORKSHOPNAME = "کارگاه";
        head.ELAMH_RCVNO = "1";
        head.ELAMH_LSTMON = "0405"; // نگا. RevenueElamLetterTests: قاعدهٔ طول ۸ با ماه دورقمی برقرار نیست
        var d = head.TB_ELAMDETAILs.Single();
        d.DEBTOR = 100;
        d.CREDITOR = 0;
        return head;
    }

    [Fact]
    public async Task Final_confirm_revenue_sends_letter_and_stores_contract_number()
    {
        var head = ReadyRevenue();
        _sender.Setup(s => s.SendAsync(It.IsAny<RevenueElamLetter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("1123456789012345XYZ");

        var result = await Handlers().Handle(new ConfirmElamCommand(head.ID, true) { VahedCode = "0042" }, default);

        Assert.Null(result);
        Assert.Equal((byte)ElamWebStat.FinalConfirmDramad, head.WEB_STAT);
        Assert.Equal("123456789012", head.PEIMAN_NO);
        Assert.Empty(_addedHeads);
    }

    [Fact]
    public async Task Final_confirm_revenue_rejected_by_saba_conflicts_and_keeps_state()
    {
        var head = ReadyRevenue();
        _sender.Setup(s => s.SendAsync(It.IsAny<RevenueElamLetter>(), It.IsAny<CancellationToken>())).ReturnsAsync("0error");

        await Assert.ThrowsAsync<ElamConflictException>(() =>
            Handlers().Handle(new ConfirmElamCommand(head.ID, true) { VahedCode = "0042" }, default));
        Assert.Equal((byte)ElamWebStat.FirstConfirmDramad, head.WEB_STAT);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Final_confirm_revenue_transport_failure_becomes_conflict()
    {
        var head = ReadyRevenue();
        _sender.Setup(s => s.SendAsync(It.IsAny<RevenueElamLetter>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("down"));

        await Assert.ThrowsAsync<ElamConflictException>(() =>
            Handlers().Handle(new ConfirmElamCommand(head.ID, true) { VahedCode = "0042" }, default));
    }

    [Fact]
    public async Task Final_confirm_revenue_with_incomplete_data_does_not_call_saba()
    {
        var head = ReadyRevenue();
        head.ELAMH_WORKSHOPCODE = "123";

        await Assert.ThrowsAsync<ElamConflictException>(() =>
            Handlers().Handle(new ConfirmElamCommand(head.ID, true) { VahedCode = "0042" }, default));
        _sender.Verify(s => s.SendAsync(It.IsAny<RevenueElamLetter>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
