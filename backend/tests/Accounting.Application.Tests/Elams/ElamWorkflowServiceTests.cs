using Accounting.Application.Commands.Vouchers.Common;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Elams;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.Elams;

/// <summary>
/// گردش اعلامیه (فاز ۴۶): جهت مبالغ ردیف‌ها بر اساس ماهیت، سند موقت با ردیف رابط، شناسهٔ رابط،
/// و ساخت خودکار اعلامیهٔ رسیده در واحد مقصد.
/// </summary>
public class ElamWorkflowServiceTests
{
    private static readonly Guid RabetAccountId = Guid.NewGuid();

    private readonly Mock<IElamWorkflowRepository> _repo = new();
    private readonly Mock<IVoucherHeadRepository> _heads = new();
    private readonly Mock<IVoucherDetailRepository> _details = new();
    private readonly Mock<IVoucherTafsiliLevelGuard> _guard = new();
    private readonly Mock<ICurrentUser> _user = new();

    private readonly List<TB_VOUCHERSHEAD> _addedHeads = new();
    private readonly List<TB_VOUCHERSDETAIL> _addedLines = new();
    private readonly List<TB_VOUCHERDETAIL_LINK_TAFSILI> _addedLineLinks = new();
    private readonly List<TB_ELAMHEAD> _addedElamHeads = new();
    private readonly List<TB_ELAMDETAIL> _addedElamDetails = new();
    private readonly List<TB_ELAMDETAIL_LINK_TAFSILI> _addedElamLinks = new();
    private readonly List<TB_ATTRIBSINVOUCHER> _addedAttribs = new();

    public ElamWorkflowServiceTests()
    {
        _user.SetupGet(u => u.UserId).Returns("1234567890");
        _repo.Setup(r => r.GetRabetAccountAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string code, CancellationToken _) => new ElamRabetAccount(RabetAccountId, "90000" + code));
        _repo.Setup(r => r.AddHeadAsync(It.IsAny<TB_ELAMHEAD>(), It.IsAny<CancellationToken>()))
            .Callback<TB_ELAMHEAD, CancellationToken>((h, _) => _addedElamHeads.Add(h));
        _repo.Setup(r => r.AddDetailAsync(It.IsAny<TB_ELAMDETAIL>(), It.IsAny<CancellationToken>()))
            .Callback<TB_ELAMDETAIL, CancellationToken>((d, _) => _addedElamDetails.Add(d));
        _repo.Setup(r => r.AddDetailLinkAsync(It.IsAny<TB_ELAMDETAIL_LINK_TAFSILI>(), It.IsAny<CancellationToken>()))
            .Callback<TB_ELAMDETAIL_LINK_TAFSILI, CancellationToken>((l, _) => _addedElamLinks.Add(l));
        _repo.Setup(r => r.AddAttribInVoucherAsync(It.IsAny<TB_ATTRIBSINVOUCHER>(), It.IsAny<CancellationToken>()))
            .Callback<TB_ATTRIBSINVOUCHER, CancellationToken>((a, _) => _addedAttribs.Add(a));
        _repo.Setup(r => r.GetNextSerialAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("00421404000007");
        _heads.Setup(h => h.GetNextDocNumAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("000015");
        _heads.Setup(h => h.AddAsync(It.IsAny<TB_VOUCHERSHEAD>(), It.IsAny<CancellationToken>()))
            .Callback<TB_VOUCHERSHEAD, CancellationToken>((h, _) => _addedHeads.Add(h));
        _details.Setup(d => d.AddAsync(It.IsAny<TB_VOUCHERSDETAIL>(), It.IsAny<CancellationToken>()))
            .Callback<TB_VOUCHERSDETAIL, CancellationToken>((d, _) => _addedLines.Add(d));
        _details.Setup(d => d.AddTafsiliLinkAsync(It.IsAny<TB_VOUCHERDETAIL_LINK_TAFSILI>(), It.IsAny<CancellationToken>()))
            .Callback<TB_VOUCHERDETAIL_LINK_TAFSILI, CancellationToken>((l, _) => _addedLineLinks.Add(l));
    }

    private ElamWorkflowService Service()
        => new(_repo.Object, _heads.Object, _details.Object, _guard.Object, _user.Object);

    private static TB_ELAMHEAD Head(ElamCase elamCase, ElamWebStat stat = ElamWebStat.CreateOther) => new()
    {
        ID = Guid.NewGuid(),
        ELAMH_CASE = elamCase,
        WEB_STAT = (byte)stat,
        ELAMH_DATE = "14040510",
        ELAMH_SERIALNO = "00421404000123",
        ELAMH_SENDRCVVAHED = "0051",
        VAHEDCODE = "0042",
        YEAR = "1404",
    };

    private static TB_ELAMDETAIL Detail(decimal debtor, decimal creditor, string? attrib = null) => new()
    {
        ID = Guid.NewGuid(),
        ACCOUNTCODE_ID = Guid.NewGuid(),
        DEBTOR = debtor,
        CREDITOR = creditor,
        ELAM_ATRIBNO = attrib,
        ISDELETED = false,
    };

    [Theory]
    [InlineData((byte)1, ElamKind.Revenue)]
    [InlineData((byte)4, ElamKind.Revenue)]
    [InlineData((byte)5, ElamKind.Sent)]
    [InlineData((byte)8, ElamKind.Sent)]
    [InlineData((byte)9, ElamKind.Received)]
    [InlineData((byte)10, ElamKind.Received)]
    public void KindOf_maps_web_stat_ranges(byte webStat, ElamKind expected)
        => Assert.Equal(expected, ElamWorkflowService.KindOf(webStat));

    [Fact]
    public void KindOf_null_is_sent()
        => Assert.Equal(ElamKind.Sent, ElamWorkflowService.KindOf(null));

    [Theory]
    [InlineData(ElamWebStat.CreateOther, true)]
    [InlineData(ElamWebStat.CreateDramad, true)]
    [InlineData(ElamWebStat.VoucherOther, false)]
    [InlineData(ElamWebStat.RcvInfo, false)]
    public void IsEditable_only_in_create_states(ElamWebStat stat, bool expected)
        => Assert.Equal(expected, ElamWorkflowService.IsEditable(Head(ElamCase.Debtor, stat)));

    [Fact]
    public void IsEditable_false_once_voucher_issued()
    {
        var head = Head(ElamCase.Debtor);
        head.VOUCHERSHEAD_ID = Guid.NewGuid();
        Assert.False(ElamWorkflowService.IsEditable(head));
    }

    [Fact]
    public async Task StageDetails_debtor_case_puts_amount_on_creditor_side()
    {
        var head = Head(ElamCase.Debtor);
        await Service().StageDetailsAsync(head, new[] { new ElamDetailInput(Guid.NewGuid(), 500m, "  ", " 77 ", null) }, default);

        var d = Assert.Single(_addedElamDetails);
        Assert.Equal(0m, d.DEBTOR);
        Assert.Equal(500m, d.CREDITOR);
        Assert.Null(d.ELAMD_DESC);
        Assert.Equal("77", d.ELAM_ATRIBNO);
        Assert.Equal("0042", d.VAHEDCODE);
        Assert.Equal(head.ID, d.ELAMHEAD_ID);
    }

    [Fact]
    public async Task StageDetails_creditor_case_puts_amount_on_debtor_side_and_stages_links()
    {
        var head = Head(ElamCase.Creditor);
        var links = new[] { new VoucherDetailTafsiliLinkInput(Guid.NewGuid(), Guid.NewGuid()) };
        await Service().StageDetailsAsync(head, new[] { new ElamDetailInput(Guid.NewGuid(), 300m, "x", null, links) }, default);

        var d = Assert.Single(_addedElamDetails);
        Assert.Equal(300m, d.DEBTOR);
        Assert.Equal(0m, d.CREDITOR);
        var l = Assert.Single(_addedElamLinks);
        Assert.Equal(d.ID, l.ELAMDETAIL_ID);
        Assert.Equal(links[0].TafsiliId, l.TAFSILI_ID);
    }

    [Fact]
    public async Task StageDetails_runs_tafsili_guard_and_propagates_failure()
    {
        var accountId = Guid.NewGuid();
        _guard.Setup(g => g.EnsureSatisfiedAsync(accountId, It.IsAny<IReadOnlyList<VoucherDetailTafsiliLinkInput>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequiredTafsiliLevelMissingException(accountId, new[] { "سطح ۱" }));

        await Assert.ThrowsAsync<RequiredTafsiliLevelMissingException>(() =>
            Service().StageDetailsAsync(Head(ElamCase.Debtor), new[] { new ElamDetailInput(accountId, 1m, null, null, null) }, default));
        Assert.Empty(_addedElamDetails);
    }

    [Fact]
    public void SoftDeleteDetails_marks_active_details_and_links()
    {
        var head = Head(ElamCase.Debtor);
        var d = Detail(0, 10);
        d.TB_ELAMDETAIL_LINK_TAFSILIs.Add(new TB_ELAMDETAIL_LINK_TAFSILI { ID = Guid.NewGuid(), ISDELETED = false });
        head.TB_ELAMDETAILs.Add(d);

        Service().SoftDeleteDetails(head);

        Assert.True(d.ISDELETED);
        Assert.Equal("1234567890", d.CHANGEUSERID);
        Assert.True(d.TB_ELAMDETAIL_LINK_TAFSILIs.Single().ISDELETED);
    }

    [Fact]
    public async Task IssueVoucher_without_details_conflicts()
        => await Assert.ThrowsAsync<ElamConflictException>(() =>
            Service().IssueVoucherAsync(Head(ElamCase.Debtor), Array.Empty<TB_ELAMDETAIL>(), default));

    [Fact]
    public async Task IssueVoucher_missing_rabet_conflicts()
    {
        _repo.Setup(r => r.GetRabetAccountAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ElamRabetAccount?)null);

        var ex = await Assert.ThrowsAsync<ElamConflictException>(() =>
            Service().IssueVoucherAsync(Head(ElamCase.Debtor), new[] { Detail(0, 10) }, default));
        Assert.Contains("نوع 1", ex.PublicDetail);
    }

    [Fact]
    public async Task IssueVoucher_rabet_with_required_tafsili_conflicts()
    {
        _guard.Setup(g => g.EnsureSatisfiedAsync(RabetAccountId, It.IsAny<IReadOnlyList<VoucherDetailTafsiliLinkInput>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequiredTafsiliLevelMissingException(RabetAccountId, new[] { "سطح ۱" }));

        await Assert.ThrowsAsync<ElamConflictException>(() =>
            Service().IssueVoucherAsync(Head(ElamCase.Debtor), new[] { Detail(0, 10) }, default));
        Assert.Empty(_addedHeads);
    }

    [Fact]
    public async Task IssueVoucher_debtor_case_builds_temporary_balanced_voucher_with_rabet_debit()
    {
        var head = Head(ElamCase.Debtor);
        _repo.Setup(r => r.GetVahedNameAsync("0051", It.IsAny<CancellationToken>())).ReturnsAsync("واحد مقصد");

        var docNum = await Service().IssueVoucherAsync(head, new[] { Detail(0, 100), Detail(0, 250) }, default);

        Assert.Equal("000015", docNum);
        var v = Assert.Single(_addedHeads);
        Assert.Equal(DocLife.Temporary, v.DOCLIFE);
        Assert.True(v.ISAUTOMATIC);
        Assert.Equal("14040510", v.DATE_DOC);
        Assert.Contains("واحد مقصد", v.HEAD_DESC);
        Assert.Equal(3, _addedLines.Count);
        Assert.Equal(new int?[] { 1, 2, 3 }, _addedLines.Select(l => l.RADIF));
        var rabet = _addedLines.Last();
        Assert.Equal(RabetAccountId, rabet.ACCOUNT_ID);
        Assert.Equal(350m, rabet.DEBTOR);
        Assert.Equal(0m, rabet.CREDITOR);
        Assert.Equal(_addedLines.Sum(l => l.DEBTOR), _addedLines.Sum(l => l.CREDITOR));
        Assert.Equal(v.ID, head.VOUCHERSHEAD_ID);
        Assert.Equal("900001", head.ELAMH_CODE);
    }

    [Fact]
    public async Task IssueVoucher_creditor_case_puts_rabet_on_credit()
    {
        await Service().IssueVoucherAsync(Head(ElamCase.Creditor), new[] { Detail(40, 0), Detail(60, 0) }, default);

        var rabet = _addedLines.Last();
        Assert.Equal(0m, rabet.DEBTOR);
        Assert.Equal(100m, rabet.CREDITOR);
    }

    [Fact]
    public async Task IssueVoucher_revenue_kind_uses_rabet_type_3()
    {
        await Service().IssueVoucherAsync(Head(ElamCase.Debtor, ElamWebStat.CreateDramad), new[] { Detail(0, 1) }, default);
        _repo.Verify(r => r.GetRabetAccountAsync("3", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task IssueVoucher_copies_active_tafsili_links_only()
    {
        var d = Detail(0, 10);
        d.TB_ELAMDETAIL_LINK_TAFSILIs.Add(new TB_ELAMDETAIL_LINK_TAFSILI { TAFSILI_ID = Guid.NewGuid(), LEVEL_ID = Guid.NewGuid(), ISDELETED = false });
        d.TB_ELAMDETAIL_LINK_TAFSILIs.Add(new TB_ELAMDETAIL_LINK_TAFSILI { TAFSILI_ID = Guid.NewGuid(), LEVEL_ID = Guid.NewGuid(), ISDELETED = true });
        d.TB_ELAMDETAIL_LINK_TAFSILIs.Add(new TB_ELAMDETAIL_LINK_TAFSILI { TAFSILI_ID = null, LEVEL_ID = Guid.NewGuid(), ISDELETED = false });

        await Service().IssueVoucherAsync(Head(ElamCase.Debtor), new[] { d }, default);

        var link = Assert.Single(_addedLineLinks);
        Assert.Equal(_addedLines[0].ID, link.VOUCHERSDETAIL_ID);
    }

    [Fact]
    public async Task IssueVoucher_rabet_attribute_is_counter_unit_plus_3_plus_serial_tail()
    {
        var rabetDefinition = Guid.NewGuid();
        _repo.Setup(r => r.GetAttribDefinitionIdAsync(RabetAccountId, "0042", "1404", It.IsAny<CancellationToken>()))
            .ReturnsAsync(rabetDefinition);

        await Service().IssueVoucherAsync(Head(ElamCase.Debtor), new[] { Detail(0, 10) }, default);

        var attr = Assert.Single(_addedAttribs);
        Assert.Equal(rabetDefinition, attr.ATTRIBFORACCOUNTCODE_ID);
        // سریال 00421404000123 ⇒ پنج رقم از اندیس ۹ = "00123"
        Assert.Equal("0051300123", attr.ATTRIBUTEVALUE);
    }

    [Fact]
    public async Task IssueVoucher_line_attribute_skipped_when_account_has_no_definition()
    {
        await Service().IssueVoucherAsync(Head(ElamCase.Debtor), new[] { Detail(0, 10, attrib: "55") }, default);
        Assert.Empty(_addedAttribs);
    }

    [Fact]
    public async Task CreateReceived_without_target_unit_conflicts()
    {
        var sender = Head(ElamCase.Debtor, ElamWebStat.FirstConfirmOther);
        sender.ELAMH_SENDRCVVAHED = null;
        await Assert.ThrowsAsync<ElamConflictException>(() => Service().CreateReceivedAsync(sender, new[] { Detail(0, 1) }, default));
    }

    [Fact]
    public async Task CreateReceived_copies_details_into_target_unit_and_issues_voucher_there()
    {
        var sender = Head(ElamCase.Debtor, ElamWebStat.FirstConfirmOther);
        var d = Detail(0, 80);
        d.TB_ELAMDETAIL_LINK_TAFSILIs.Add(new TB_ELAMDETAIL_LINK_TAFSILI { TAFSILI_ID = Guid.NewGuid(), LEVEL_ID = Guid.NewGuid(), ISDELETED = false });

        var rcv = await Service().CreateReceivedAsync(sender, new[] { d }, default);

        Assert.Equal("0051", rcv.VAHEDCODE);
        Assert.Equal("0042", rcv.ELAMH_SENDRCVVAHED);
        Assert.Equal(sender.ID, rcv.ELAMSENDERID);
        Assert.Equal("00421404000007", rcv.ELAMH_SERIALNO);
        Assert.Equal((byte)ElamWebStat.ConfirmRcvInfo, rcv.WEB_STAT);
        Assert.Same(rcv, Assert.Single(_addedElamHeads));

        var copy = Assert.Single(_addedElamDetails);
        Assert.Equal("0051", copy.VAHEDCODE);
        Assert.Equal(80m, copy.CREDITOR);
        Assert.Equal("0051", Assert.Single(_addedElamLinks).VAHEDCODE);

        var v = Assert.Single(_addedHeads);
        Assert.Equal("0051", v.VAHEDCODE);
        Assert.Equal(v.ID, rcv.VOUCHERSHEAD_ID);
        _repo.Verify(r => r.GetRabetAccountAsync("2", It.IsAny<CancellationToken>()));
        Assert.Single(_addedLineLinks);
    }
}
