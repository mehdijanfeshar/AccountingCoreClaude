using Accounting.Application.Common.Interfaces;
using Accounting.Application.Vouchers.Inbox;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.Vouchers;

/// <summary>دریافت سند از سایر سیستم‌ها از کافکا — فقط کنترل ساختاری و نشاندن در جدول موقت.</summary>
public class SystemVoucherIngestServiceTests
{
    private readonly Mock<ISystemVoucherInboxRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private TB_TMP_VOUCHERHEAD? _head;
    private IReadOnlyList<TB_TMP_VOUCHERSDETAIL>? _lines;

    public SystemVoucherIngestServiceTests()
    {
        _repo.Setup(r => r.VahedExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _repo.Setup(r => r.ResolveSysTypeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Guid.NewGuid(), "حقوق"));
        _repo.Setup(r => r.StageAsync(It.IsAny<TB_TMP_VOUCHERHEAD>(), It.IsAny<IReadOnlyList<TB_TMP_VOUCHERSDETAIL>>(), It.IsAny<CancellationToken>()))
            .Callback<TB_TMP_VOUCHERHEAD, IReadOnlyList<TB_TMP_VOUCHERSDETAIL>, CancellationToken>((h, l, _) => (_head, _lines) = (h, l));
    }

    private SystemVoucherIngestService Service() => new(_repo.Object, _uow.Object);

    private static SystemVoucherMessage Valid() => new()
    {
        VahedCode = " 0101 ",
        Year = "1405",
        DateDoc = "14050720",
        HeadDesc = " حقوق مهر ",
        SysType = "P",
        SourceId = Guid.NewGuid(),
        Details =
        [
            new() { MoinCode = "810101", TafsiliCode1 = " 1001 ", Debtor = 1_000, Description = "حقوق" },
            new() { MoinCode = "310101", Creditor = 1_000, Radif = 5, TafsiliCode2 = "  " },
        ],
    };

    private void AssertNothingSaved()
    {
        _repo.Verify(r => r.StageAsync(It.IsAny<TB_TMP_VOUCHERHEAD>(), It.IsAny<IReadOnlyList<TB_TMP_VOUCHERSDETAIL>>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ValidMessage_IsStagedAsTemporaryVoucher()
    {
        var res = await Service().IngestAsync(Valid(), default);

        Assert.Equal(SystemVoucherIngestStatus.Created, res.Status);
        Assert.Equal(_head!.ID, res.TmpVoucherId);
        Assert.Equal("0101", _head.VAHEDCODE);
        Assert.Equal("حقوق مهر", _head.HEAD_DESC);
        Assert.Equal("P", _head.SYS_TYPE);
        Assert.Equal(SystemVoucherIngestService.SystemUserId, _head.ADDUSERID);
        Assert.False(_head.ISDELETED);

        Assert.Equal(2, _lines!.Count);
        Assert.Equal(1, _lines[0].RADIF);          // ردیف خالی ⇒ ترتیب پیام
        Assert.Equal(5, _lines[1].RADIF);
        Assert.Equal("1001", _lines[0].TAFSILI_CODE1);
        Assert.Null(_lines[1].TAFSILI_CODE2);      // فاصلهٔ خالی ⇒ null
        Assert.Equal(0, _lines[0].CREDITOR);
        Assert.All(_lines, l => Assert.Equal(_head.ID, l.TMPVOUCHERHEAD_ID));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LegacyDocTypeField_IsUsedWhenSysTypeMissing()
    {
        var m = Valid();
        m.SysType = null;
        m.DocType = "H";
        await Service().IngestAsync(m, default);
        Assert.Equal("H", _head!.SYS_TYPE);
    }

    [Fact]
    public async Task DuplicateSource_ReturnsExistingWithoutSaving()
    {
        var existing = Guid.NewGuid();
        _repo.Setup(r => r.FindStagedBySourceAsync(It.IsAny<Guid>(), "0101", It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var res = await Service().IngestAsync(Valid(), default);

        Assert.Equal(SystemVoucherIngestStatus.Duplicate, res.Status);
        Assert.Equal(existing, res.TmpVoucherId);
        AssertNothingSaved();
    }

    [Fact]
    public async Task UnknownUnit_IsRejected()
    {
        _repo.Setup(r => r.VahedExistsAsync("0101", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var res = await Service().IngestAsync(Valid(), default);
        Assert.Equal(SystemVoucherIngestStatus.Rejected, res.Status);
        Assert.Contains(res.Errors, e => e.Contains("0101"));
        AssertNothingSaved();
    }

    [Fact]
    public async Task UnknownSysType_IsRejected()
    {
        _repo.Setup(r => r.ResolveSysTypeAsync("P", It.IsAny<CancellationToken>())).ReturnsAsync(((Guid, string?)?)null);
        var res = await Service().IngestAsync(Valid(), default);
        Assert.Equal(SystemVoucherIngestStatus.Rejected, res.Status);
        AssertNothingSaved();
    }

    public static TheoryData<Action<SystemVoucherMessage>, string> StructuralErrors => new()
    {
        { m => m.VahedCode = null, "کد واحد" },
        { m => m.VahedCode = "01011", "کد واحد" },
        { m => m.Year = "05", "سال مالی" },
        { m => m.DateDoc = "1405-07-20", "تاریخ سند" },
        { m => m.HeadDesc = new string('x', 251), "شرح سند" },
        { m => m.SysType = "PP", "نوع سیستم" },
        { m => { m.SysType = null; m.DocType = null; }, "نوع سیستم" },
        { m => m.Details = [], "ردیف ندارد" },
        { m => m.Details[0].MoinCode = " ", "کد معین خالی" },
        { m => m.Details[0].MoinCode = "8101011", "کد معین" },
        { m => m.Details[0].TafsiliCode7 = "123456789012", "سطح 7" },
        { m => m.Details[0].Debtor = -1, "منفی" },
        { m => m.Details[0].Debtor = 10.5m, "ریال صحیح" },
        { m => m.Details[0].AttribValue = new string('1', 21), "مقدار ویژگی" },
    };

    [Theory]
    [MemberData(nameof(StructuralErrors))]
    public async Task StructuralErrors_AreRejectedBeforeAnyLookup(Action<SystemVoucherMessage> breakIt, string fragment)
    {
        var m = Valid();
        breakIt(m);

        var res = await Service().IngestAsync(m, default);

        Assert.Equal(SystemVoucherIngestStatus.Rejected, res.Status);
        Assert.Contains(res.Errors, e => e.Contains(fragment));
        AssertNothingSaved();
    }

    [Fact]
    public async Task AllStructuralErrors_AreReportedTogether()
    {
        var m = Valid();
        m.Year = null;
        m.Details[0].Debtor = -1;
        m.Details[1].MoinCode = null;
        var res = await Service().IngestAsync(m, default);
        Assert.True(res.Errors.Count >= 3);
        Assert.Contains(res.Errors, e => e.StartsWith("ردیف 5"));
    }
}
