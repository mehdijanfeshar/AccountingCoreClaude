using Accounting.Application.Commands.Treasury.Common;
using Accounting.Application.Commands.Vouchers.Common;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Queries.BankAccounts;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.Treasury.Commands.Common;

/// <summary>سند خودکار انتقال: بدهکار بانک مقصد، بستانکار بانک مبدأ، موقت و خودکار، تراز.</summary>
public sealed class TransferVoucherBuilderTests
{
    private const string Vahed = "V1";
    private static readonly Guid SrcId = Guid.NewGuid();
    private static readonly Guid DstId = Guid.NewGuid();
    private static readonly Guid SrcAccount = Guid.NewGuid();
    private static readonly Guid DstAccount = Guid.NewGuid();

    private readonly Mock<IVoucherHeadRepository> _heads = new();
    private readonly Mock<IVoucherDetailRepository> _details = new();
    private readonly Mock<IVoucherTafsiliLevelGuard> _guard = new();
    private readonly Mock<IBankAccountReadRepository> _banks = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly List<TB_VOUCHERSDETAIL> _addedDetails = new();
    private TB_VOUCHERSHEAD? _head;

    public TransferVoucherBuilderTests()
    {
        _user.SetupGet(u => u.UserId).Returns("treasurer");
        _heads.Setup(h => h.GetNextDocNumAsync(Vahed, "1405", It.IsAny<CancellationToken>())).ReturnsAsync("42");
        _heads.Setup(h => h.AddAsync(It.IsAny<TB_VOUCHERSHEAD>(), It.IsAny<CancellationToken>()))
            .Callback<TB_VOUCHERSHEAD, CancellationToken>((h, _) => _head = h).Returns(Task.CompletedTask);
        _details.Setup(d => d.AddAsync(It.IsAny<TB_VOUCHERSDETAIL>(), It.IsAny<CancellationToken>()))
            .Callback<TB_VOUCHERSDETAIL, CancellationToken>((d, _) => _addedDetails.Add(d)).Returns(Task.CompletedTask);
        _banks.Setup(b => b.GetByIdAsync(SrcId, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(Bank(SrcId, SrcAccount));
        _banks.Setup(b => b.GetByIdAsync(DstId, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(Bank(DstId, DstAccount));
    }

    private static BankAccountDto Bank(Guid id, Guid? accountCode) => new(
        id, "1", "h", null, null, null, null, null, null, accountCode, Vahed, null, null, null, null, null, false,
        new List<BankAccountTafsiliLinkDto> { new(Guid.NewGuid(), Guid.NewGuid()) });

    private TransferVoucherBuilder Sut() => new(_heads.Object, _details.Object, _guard.Object, _banks.Object, _user.Object);

    private static TB_TR_TRANSFER Transfer() => new()
    {
        ID = Guid.NewGuid(), CODE = "TRF-000001", AMOUNT = 750m, TRANSFER_DATE = "14050105",
        SOURCE_BANK_ACCOUNT_ID = SrcId, DEST_BANK_ACCOUNT_ID = DstId,
    };

    [Fact]
    public async Task Build_StagesBalancedTemporaryAutomaticVoucher()
    {
        var result = await Sut().BuildAndStageAsync(Transfer(), Vahed);

        Assert.Equal("42", result.DocNum);
        Assert.Equal("1405", result.Year);
        Assert.Equal("14050105", result.DateDoc);
        Assert.Equal(_head!.ID, result.VoucherHeadId);
        Assert.Equal(DocLife.Temporary, _head.DOCLIFE);
        Assert.True(_head.ISAUTOMATIC);
        Assert.Equal(Vahed, _head.VAHEDCODE);
        Assert.Equal("treasurer", _head.ADDUSERID);
        Assert.Equal(2, _addedDetails.Count);

        var debit = _addedDetails.Single(d => d.DEBTOR > 0);
        var credit = _addedDetails.Single(d => d.CREDITOR > 0);
        Assert.Equal(DstAccount, debit.ACCOUNT_ID);
        Assert.Equal(750m, debit.DEBTOR);
        Assert.Equal(SrcAccount, credit.ACCOUNT_ID);
        Assert.Equal(750m, credit.CREDITOR);
        Assert.Equal(_addedDetails.Sum(d => d.DEBTOR), _addedDetails.Sum(d => d.CREDITOR));
        Assert.Equal(new int?[] { 1, 2 }, _addedDetails.Select(d => d.RADIF).ToArray<int?>());
        Assert.All(_addedDetails, d => Assert.Equal(_head.ID, d.VOUCHERSHEAD_ID));
    }

    [Fact]
    public async Task Build_ResultLinesMirrorDetailsAndTafsiliLinks()
    {
        var result = await Sut().BuildAndStageAsync(Transfer(), Vahed);

        Assert.Equal(2, result.Lines.Count);
        Assert.Equal(DstAccount, result.Lines[0].AccountCodeId);
        Assert.Equal(750m, result.Lines[0].Debit);
        Assert.Equal(SrcAccount, result.Lines[1].AccountCodeId);
        Assert.Equal(750m, result.Lines[1].Credit);
        Assert.Single(result.Lines[0].TafsiliLinks);
        _details.Verify(d => d.AddTafsiliLinkAsync(It.IsAny<TB_VOUCHERDETAIL_LINK_TAFSILI>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Build_SourceBankMissing_ThrowsNotFound_NothingStaged()
    {
        _banks.Setup(b => b.GetByIdAsync(SrcId, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync((BankAccountDto?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => Sut().BuildAndStageAsync(Transfer(), Vahed));

        _heads.Verify(h => h.AddAsync(It.IsAny<TB_VOUCHERSHEAD>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Build_DestBankWithoutAccountCode_ThrowsNotFound()
    {
        _banks.Setup(b => b.GetByIdAsync(DstId, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(Bank(DstId, null));

        await Assert.ThrowsAsync<NotFoundException>(() => Sut().BuildAndStageAsync(Transfer(), Vahed));
    }

    [Fact]
    public async Task Build_TafsiliRuleViolation_IsTranslatedToTreasuryException_BeforeStaging()
    {
        _guard.Setup(g => g.EnsureSatisfiedAsync(DstAccount, It.IsAny<IReadOnlyCollection<VoucherDetailTafsiliLinkInput>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequiredTafsiliLevelMissingException(DstAccount, new[] { "مرکز هزینه" }));

        await Assert.ThrowsAsync<TreasuryVoucherTafsiliMissingException>(() => Sut().BuildAndStageAsync(Transfer(), Vahed));

        _heads.Verify(h => h.AddAsync(It.IsAny<TB_VOUCHERSHEAD>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
