using Accounting.Application.Accounts.Commands.CreateAccountCode;
using Accounting.Application.Accounts.Commands.UpdateAccountCode;
using Accounting.Application.AccountCodes.Queries.GetTafsiliLevels;
using Accounting.Application.ChequeTypes.Commands.CreateChequeType;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Treasury.Commands.Common;
using Accounting.Application.Vouchers.Commands.Common;
using Accounting.Application.Vouchers.Commands.CreateVoucherDetail;
using Accounting.Application.BankAccounts.Queries;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.Phase52B;

/// <summary>قاعده‌های فاز ۵۲-ب: سلسله‌مراتب کدینگ، شمارهٔ ردیف سمت سرور، تفصیلی مجاز ردیف، تاریخ وصول، بازهٔ مختصات چک.</summary>
public sealed class Phase52BRulesTests
{
    private static Mock<ICurrentUser> User()
    {
        var user = new Mock<ICurrentUser>();
        user.SetupGet(u => u.UserId).Returns("u1");
        return user;
    }

    // ───────────── سلسله‌مراتب کدینگ ─────────────

    private static CreateAccountCodeCommand Account(TypeCodes type, Guid? parentId, string code) => new(
        TypeCode: type, ParentId: parentId, AccCode: code, AccCodeName: "x", TypeActivity: TypeActivity.Debit,
        SourceAndConsumeId: null, IdentyGroupsId: null, TypeAccCode: TypeAccCode.Permanent, MoInforClose: null, TypeAction: null);

    private static TB_ACCOUNTCODE Node(Guid id, TypeCodes type, string code, bool deleted = false) => new()
    {
        ID = id, TYPECODE = type, ACCCODE = code, ISDELETED = deleted, ADDUSERID = "c", CREATEDDATE = DateTime.UtcNow,
    };

    private static async Task<Exception?> CreateAsync(CreateAccountCodeCommand command, TB_ACCOUNTCODE? parent = null)
    {
        var repo = new Mock<IAccountCodeRepository>();
        if (parent is not null)
            repo.Setup(r => r.GetForUpdateAsync(parent.ID, It.IsAny<CancellationToken>())).ReturnsAsync(parent);
        var handler = new CreateAccountCodeCommandHandler(repo.Object, new Mock<IUnitOfWork>().Object, User().Object);
        return await Record.ExceptionAsync(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Group_WithParent_IsRejected()
        => Assert.IsType<BusinessRuleException>(await CreateAsync(Account(TypeCodes.Group, Guid.NewGuid(), "10")));

    [Fact]
    public async Task Kol_WithoutParent_IsRejected()
        => Assert.IsType<BusinessRuleException>(await CreateAsync(Account(TypeCodes.Kol, null, "1010")));

    [Fact]
    public async Task Moin_UnderGroup_IsRejected()
    {
        var group = Node(Guid.NewGuid(), TypeCodes.Group, "10");
        Assert.IsType<BusinessRuleException>(await CreateAsync(Account(TypeCodes.Moin, group.ID, "101010"), group));
    }

    [Fact]
    public async Task Moin_WhoseCodeDoesNotStartWithKolCode_IsRejected()
    {
        var kol = Node(Guid.NewGuid(), TypeCodes.Kol, "1022");
        Assert.IsType<BusinessRuleException>(await CreateAsync(Account(TypeCodes.Moin, kol.ID, "005000"), kol));
    }

    [Fact]
    public async Task Moin_WithCodeEqualToParent_IsRejected()
    {
        var kol = Node(Guid.NewGuid(), TypeCodes.Kol, "1022");
        Assert.IsType<BusinessRuleException>(await CreateAsync(Account(TypeCodes.Moin, kol.ID, "1022"), kol));
    }

    [Fact]
    public async Task Moin_UnderDeletedKol_IsRejected()
    {
        var kol = Node(Guid.NewGuid(), TypeCodes.Kol, "1022", deleted: true);
        Assert.IsType<BusinessRuleException>(await CreateAsync(Account(TypeCodes.Moin, kol.ID, "102201"), kol));
    }

    [Fact]
    public async Task Moin_UnderKolWithPrefix_IsAccepted()
    {
        var kol = Node(Guid.NewGuid(), TypeCodes.Kol, "1022");
        Assert.Null(await CreateAsync(Account(TypeCodes.Moin, kol.ID, "102201"), kol));
    }

    [Fact]
    public async Task Update_LegacyInvalidAccount_OnlyNameChanged_IsNotRechecked()
    {
        // معین 005000 زیر کل 1022 — دادهٔ واقعی توسعه؛ تغییر نام نباید قاعده را بیدار کند.
        var kolId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var existing = Node(id, TypeCodes.Moin, "005000");
        existing.PARENTID = kolId;
        var repo = new Mock<IAccountCodeRepository>(MockBehavior.Strict);
        repo.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        var handler = new UpdateAccountCodeCommandHandler(repo.Object, new Mock<IUnitOfWork>().Object, User().Object);

        await handler.Handle(
            new UpdateAccountCodeCommand(id, TypeCodes.Moin, kolId, "005000", "نام تازه", TypeActivity.Debit,
                null, null, TypeAccCode.Permanent, null, null),
            CancellationToken.None);

        Assert.Equal("نام تازه", existing.ACCCODENAME);
    }

    [Fact]
    public async Task Update_ChangingParentToWrongType_IsRejected()
    {
        var group = Node(Guid.NewGuid(), TypeCodes.Group, "10");
        var id = Guid.NewGuid();
        var existing = Node(id, TypeCodes.Moin, "102201");
        existing.PARENTID = Guid.NewGuid();
        var repo = new Mock<IAccountCodeRepository>();
        repo.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        repo.Setup(r => r.GetForUpdateAsync(group.ID, It.IsAny<CancellationToken>())).ReturnsAsync(group);
        var handler = new UpdateAccountCodeCommandHandler(repo.Object, new Mock<IUnitOfWork>().Object, User().Object);

        var ex = await Record.ExceptionAsync(() => handler.Handle(
            new UpdateAccountCodeCommand(id, TypeCodes.Moin, group.ID, "102201", "x", TypeActivity.Debit,
                null, null, TypeAccCode.Permanent, null, null),
            CancellationToken.None));

        Assert.IsType<BusinessRuleException>(ex);
    }

    // ───────────── شمارهٔ ردیف سمت سرور ─────────────

    private static CreateVoucherDetailCommand Line(Guid headId, int? radif) => new(
        headId, null, null, null, null, null, "d", radif, 100m, null, "1405") { VahedCode = "0001" };

    private static (CreateVoucherDetailCommandHandler handler, List<TB_VOUCHERSDETAIL> staged) LineHandler(
        Guid headId, params int?[] existingRadifs)
    {
        var heads = new Mock<IVoucherHeadRepository>();
        heads.Setup(r => r.GetForUpdateAsync(headId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TB_VOUCHERSHEAD { ID = headId, VAHEDCODE = "0001", YEAR = "1405", DOCLIFE = DocLife.Draft });
        var details = new Mock<IVoucherDetailRepository>();
        details.Setup(r => r.GetActiveByHeadAsync(headId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRadifs.Select(r => new TB_VOUCHERSDETAIL { ID = Guid.NewGuid(), RADIF = r }).ToList());
        var staged = new List<TB_VOUCHERSDETAIL>();
        details.Setup(r => r.AddAsync(It.IsAny<TB_VOUCHERSDETAIL>(), It.IsAny<CancellationToken>()))
            .Callback<TB_VOUCHERSDETAIL, CancellationToken>((d, _) => staged.Add(d));
        var handler = new CreateVoucherDetailCommandHandler(
            heads.Object, details.Object, new Mock<IUnitOfWork>().Object, User().Object,
            Vouchers.Commands.Common.TafsiliLevelGuards.Permissive());
        return (handler, staged);
    }

    [Fact]
    public async Task CreateLine_WithoutRadif_GetsLastPlusOne()
    {
        var headId = Guid.NewGuid();
        var (handler, staged) = LineHandler(headId, 1, 7, null, 3);
        await handler.Handle(Line(headId, null), CancellationToken.None);
        Assert.Equal(8, Assert.Single(staged).RADIF);
    }

    [Fact]
    public async Task CreateLine_OnEmptyVoucher_GetsOne()
    {
        var headId = Guid.NewGuid();
        var (handler, staged) = LineHandler(headId);
        await handler.Handle(Line(headId, null), CancellationToken.None);
        Assert.Equal(1, Assert.Single(staged).RADIF);
    }

    [Fact]
    public async Task CreateLine_WithExplicitRadif_FromAtomicSave_KeepsIt()
    {
        var headId = Guid.NewGuid();
        var (handler, staged) = LineHandler(headId, 1, 2);
        await handler.Handle(Line(headId, 5), CancellationToken.None);
        Assert.Equal(5, Assert.Single(staged).RADIF);
    }

    // ───────────── تفصیلی مجاز ردیف (ریسک #۹/#۱۴) ─────────────

    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid LevelId = Guid.NewGuid();

    private static VoucherTafsiliLevelGuard Guard(params Guid[] selectable)
    {
        var lookup = new Mock<ITafsiliLookupReadRepository>();
        lookup.Setup(r => r.GetSelectableTafsiliIdsAsync(AccountId, LevelId, It.IsAny<IReadOnlyCollection<Guid>>(), "0001", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, Guid _, IReadOnlyCollection<Guid> ids, string _, CancellationToken _) =>
                ids.Where(selectable.Contains).ToHashSet());
        lookup.Setup(r => r.GetActiveLevelsAsync(AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TafsiliLevelDto> { new(LevelId, 1, "سطح ۱", true) });
        return new VoucherTafsiliLevelGuard(lookup.Object);
    }

    [Fact]
    public async Task Tafsili_NotOfferedForAccountLevelUnit_IsRejected()
    {
        var bad = Guid.NewGuid();
        var ex = await Record.ExceptionAsync(() => Guard(Guid.NewGuid())
            .EnsureTafsiliSelectableAsync(AccountId, [new VoucherDetailTafsiliLinkInput(bad, LevelId)], "0001"));
        Assert.IsType<BusinessRuleException>(ex);
    }

    [Fact]
    public async Task Tafsili_Offered_IsAccepted()
    {
        var ok = Guid.NewGuid();
        await Guard(ok).EnsureTafsiliSelectableAsync(AccountId, [new VoucherDetailTafsiliLinkInput(ok, LevelId)], "0001");
    }

    [Fact]
    public async Task Tafsili_AlreadyOnTheLine_IsNotRechecked()
    {
        var legacy = Guid.NewGuid();
        await Guard().EnsureTafsiliSelectableAsync(
            AccountId, [new VoucherDetailTafsiliLinkInput(legacy, LevelId)], "0001", alreadyLinkedTafsiliIds: [legacy]);
    }

    // ───────────── تاریخ وصول در مغایرت بانکی خزانه ─────────────

    [Fact]
    public async Task ManualMatch_SetsReceivedDateOnLineCheque_AndUnmatchClearsIt()
    {
        var accountCodeId = Guid.NewGuid();
        var detailId = Guid.NewGuid();
        var checkId = Guid.NewGuid();
        var statement = new TB_TR_BANK_STATEMENT { ID = Guid.NewGuid(), BANK_ACCOUNT_ID = Guid.NewGuid(), CODE = "S", FROM_DATE = "14050101", TO_DATE = "14050131", ADDUSERID = "u" };
        var line = new TB_TR_BANK_STATEMENT_LINE
        {
            ID = Guid.NewGuid(), STATEMENT_ID = statement.ID, LINE_DATE = "14050115", WITHDRAWAL = 500m,
            MATCH_STATE = BankStatementLineMatchState.Unmatched, ADDUSERID = "u", VAHEDCODE = "0001",
        };

        var details = new Mock<IVoucherDetailRepository>();
        details.Setup(r => r.GetForUpdateAsync(detailId, "0001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TB_VOUCHERSDETAIL { ID = detailId, ACCOUNT_ID = accountCodeId, CREDITOR = 500m, CHECK_ID = checkId });
        var accounts = new Mock<IBankAccountReadRepository>();
        accounts.Setup(r => r.GetByIdAsync(statement.BANK_ACCOUNT_ID, "0001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BankAccountDto(statement.BANK_ACCOUNT_ID, "1", "h", null, null, null, null, null, null,
                accountCodeId, "0001", null, null, null, null, null, false, []));
        var lines = new Mock<ITreasuryBankStatementLineRepository>();
        lines.Setup(r => r.GetMatchedVoucherDetailIdsAsync("0001", It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Guid>());
        var cards = new Mock<IBankCardRepository>();

        var service = new BankStatementManualMatchService(details.Object, accounts.Object, lines.Object, User().Object, cards.Object);

        await service.MatchAsync(statement, line, detailId, "0001");
        cards.Verify(c => c.SetCheckReceivedDateAsync(checkId, "14050115", It.IsAny<CancellationToken>()), Times.Once);

        await service.UnmatchAsync(line);
        cards.Verify(c => c.SetCheckReceivedDateAsync(checkId, null, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Null(line.MATCHED_VOUCHERDETAIL_ID);
    }

    // ───────────── بازهٔ مختصات چک (ریسک #۲۴) ─────────────

    [Theory]
    [InlineData((short)300, (short)0, true)]   // بیش از ۲۵۵ — قبلاً سرریز byte
    [InlineData((short)1000, (short)0, false)] // طول/عرض NUMBER(3)
    [InlineData((short)100, (short)9999, true)]
    [InlineData((short)100, (short)-1, false)]
    public void ChequeTypeValidator_EnforcesColumnPrecision(short width, short left, bool valid)
    {
        var command = (CreateChequeTypeCommand)Activator.CreateInstance(
            typeof(CreateChequeTypeCommand),
            typeof(CreateChequeTypeCommand).GetConstructors()[0].GetParameters()
                .Select(p => p.Name switch
                {
                    "ChequeTypeTitle" => (object?)"t",
                    "ChequeWidth" => width,
                    "ChequeAdateLeft" => left,
                    "Year" => "1405",
                    _ => p.ParameterType.IsValueType && Nullable.GetUnderlyingType(p.ParameterType) is null
                        ? Activator.CreateInstance(p.ParameterType)
                        : null,
                })
                .ToArray())!;

        var result = new CreateChequeTypeCommandValidator().Validate(command with { VahedCode = "0001" });

        Assert.Equal(valid, result.Errors.All(e => e.PropertyName is not ("ChequeWidth" or "ChequeAdateLeft")));
    }
}
