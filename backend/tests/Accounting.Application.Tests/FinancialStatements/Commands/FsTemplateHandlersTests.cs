using Accounting.Application.Commands.FinancialStatements.ActivateFsTemplateVersion;
using Accounting.Application.Commands.FinancialStatements.AddFsTemplateRow;
using Accounting.Application.Commands.FinancialStatements.Common;
using Accounting.Application.Commands.FinancialStatements.CreateFsTemplate;
using Accounting.Application.Commands.FinancialStatements.DeleteFsTemplate;
using Accounting.Application.Commands.FinancialStatements.DeleteFsTemplateVersion;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.FinancialStatements;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using FluentValidation;
using Moq;

namespace Accounting.Application.Tests.FinancialStatements.Commands;

/// <summary>Handlerهای قالب: ساخت، حذف، حذف نسخه، افزودن ردیف، فعال‌سازی.</summary>
public sealed class FsTemplateHandlersTests
{
    private readonly Mock<IFsTemplateRepository> _repo = new();
    private readonly Mock<IFsTemplateReadRepository> _read = new();
    private readonly Mock<IFsUnitScopeProvider> _scopes = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _user = new();

    public FsTemplateHandlersTests()
    {
        _user.SetupGet(u => u.UserId).Returns("editor");
        UseScope(FsTestData.ProvinceScope());
        _read.Setup(r => r.GetTemplateCodesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<string>());
        _repo.Setup(r => r.GetVersionsForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<TB_FS_TEMPLATE_VERSION>());
    }

    private void UseScope(FsUnitScope scope)
        => _scopes.Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(scope);

    // ───────── Create ─────────

    private CreateFsTemplateCommandHandler Create() => new(_repo.Object, _scopes.Object, _uow.Object, _user.Object);

    private static CreateFsTemplateCommand NewTemplate(bool shared = false, FsStatementType type = FsStatementType.NetAssets, string code = "PENSION.NET")
        => new(FsFramework.Pension, code, "  خالص دارایی‌ها  ", "  ", type, 10, shared, " P.X ", " A01 ", " T01 ") { VahedCode = "10" };

    [Fact]
    public async Task Create_Private_BuildsTemplateAndDraftVersion1()
    {
        TB_FS_TEMPLATE? template = null;
        TB_FS_TEMPLATE_VERSION? version = null;
        _repo.Setup(r => r.AddTemplateAsync(It.IsAny<TB_FS_TEMPLATE>(), It.IsAny<CancellationToken>())).Callback<TB_FS_TEMPLATE, CancellationToken>((t, _) => template = t);
        _repo.Setup(r => r.AddVersionAsync(It.IsAny<TB_FS_TEMPLATE_VERSION>(), It.IsAny<CancellationToken>())).Callback<TB_FS_TEMPLATE_VERSION, CancellationToken>((v, _) => version = v);

        var result = await Create().Handle(NewTemplate(), default);

        Assert.Equal("10", template!.VAHEDCODE);
        Assert.Equal("خالص دارایی‌ها", template.TITLE_FA);
        Assert.Null(template.TITLE_EN);                       // فقط فاصله
        Assert.Equal("editor", template.ADDUSERID);
        Assert.Equal(FsTemplateVersionState.Draft, version!.STATE);
        Assert.Equal(1, version.VERSION_NO);
        Assert.Equal(template.ID, version.TEMPLATE_ID);
        Assert.Equal(new CreateFsTemplateResult(template.ID, version.ID), result);
        Assert.Null(template.NOTE_PARENT_TEMPLATE_CODE);      // نوع یادداشت نیست → پاک می‌شود
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_NoteTemplate_KeepsNoteLinks()
    {
        TB_FS_TEMPLATE? template = null;
        _repo.Setup(r => r.AddTemplateAsync(It.IsAny<TB_FS_TEMPLATE>(), It.IsAny<CancellationToken>())).Callback<TB_FS_TEMPLATE, CancellationToken>((t, _) => template = t);

        await Create().Handle(NewTemplate(type: FsStatementType.Note), default);

        Assert.Equal("P.X", template!.NOTE_PARENT_TEMPLATE_CODE);
        Assert.Equal("A01", template.NOTE_PARENT_ROW_CODE);
        Assert.Equal("T01", template.NOTE_TOTAL_ROW_CODE);
    }

    [Fact]
    public async Task Create_Shared_ByNonHeadquarters_IsForbidden()
    {
        await Assert.ThrowsAsync<FsAccessDeniedException>(() => Create().Handle(NewTemplate(shared: true), default));
        _repo.Verify(r => r.AddTemplateAsync(It.IsAny<TB_FS_TEMPLATE>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_Shared_ByHeadquarters_HasNullOwner()
    {
        UseScope(FsTestData.HqScope());
        TB_FS_TEMPLATE? template = null;
        _repo.Setup(r => r.AddTemplateAsync(It.IsAny<TB_FS_TEMPLATE>(), It.IsAny<CancellationToken>())).Callback<TB_FS_TEMPLATE, CancellationToken>((t, _) => template = t);

        await Create().Handle(NewTemplate(shared: true) with { VahedCode = "1" }, default);

        Assert.Null(template!.VAHEDCODE);
        _repo.Verify(r => r.TemplateCodeExistsAsync(null, "PENSION.NET", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(false, "این واحد")]
    [InlineData(true, "مشترک")]
    public async Task Create_DuplicateCode_Conflicts(bool shared, string expectedWord)
    {
        UseScope(FsTestData.HqScope());
        _repo.Setup(r => r.TemplateCodeExistsAsync(It.IsAny<string?>(), "PENSION.NET", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<FsTemplateConflictException>(() => Create().Handle(NewTemplate(shared) with { VahedCode = "1" }, default));

        Assert.Contains(expectedWord, ex.Message);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ───────── CreateTemplate validator ─────────

    [Theory]
    [InlineData("PENSION.NET_ASSETS", true)]
    [InlineData("A", true)]
    [InlineData("a.b.c", true)]
    [InlineData("1ABC", false)]
    [InlineData("A..B", false)]
    [InlineData("A.", false)]
    [InlineData("A B", false)]
    [InlineData("", false)]
    public void CreateValidator_TemplateCodeFormat(string code, bool valid)
    {
        var cmd = new CreateFsTemplateCommand(FsFramework.Pension, code, "عنوان", null, FsStatementType.NetAssets, 10);

        Assert.Equal(valid, new CreateFsTemplateCommandValidator().Validate(cmd).IsValid);
    }

    [Fact]
    public void CreateValidator_OtherRules()
    {
        var v = new CreateFsTemplateCommandValidator();
        var ok = new CreateFsTemplateCommand(FsFramework.Pension, "A", "عنوان", null, FsStatementType.NetAssets, 10);

        Assert.True(v.Validate(ok).IsValid);
        Assert.False(v.Validate(ok with { TitleFa = "" }).IsValid);
        Assert.False(v.Validate(ok with { TitleFa = new string('x', 201) }).IsValid);
        Assert.False(v.Validate(ok with { OrderNo = -1 }).IsValid);
        Assert.False(v.Validate(ok with { OrderNo = 100000 }).IsValid);
        Assert.False(v.Validate(ok with { Framework = (FsFramework)99 }).IsValid);
        Assert.False(v.Validate(ok with { StatementType = (FsStatementType)99 }).IsValid);
    }

    [Fact]
    public void CreateValidator_NoteParentTemplate_RequiresParentRow_AndValidCodes()
    {
        var v = new CreateFsTemplateCommandValidator();
        var ok = new CreateFsTemplateCommand(FsFramework.Pension, "N1", "یادداشت", null, FsStatementType.Note, 10);

        Assert.False(v.Validate(ok with { NoteParentTemplateCode = "P.X" }).IsValid);
        Assert.True(v.Validate(ok with { NoteParentTemplateCode = "P.X", NoteParentRowCode = "A01", NoteTotalRowCode = "T01" }).IsValid);
        Assert.False(v.Validate(ok with { NoteParentTemplateCode = "1bad", NoteParentRowCode = "A01" }).IsValid);
        Assert.False(v.Validate(ok with { NoteTotalRowCode = "SUM" }).IsValid);   // نام تابع
    }

    // ───────── Delete template ─────────

    private DeleteFsTemplateCommandHandler Delete() => new(_repo.Object, _scopes.Object, _uow.Object, _user.Object);

    [Fact]
    public async Task DeleteTemplate_Unknown_NotFound()
    {
        _repo.Setup(r => r.GetTemplateForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((TB_FS_TEMPLATE?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => Delete().Handle(new DeleteFsTemplateCommand(Guid.NewGuid()) { VahedCode = "10" }, default));
    }

    [Fact]
    public async Task DeleteTemplate_OfUnrelatedUnit_NotFound_OfAncestor_Forbidden()
    {
        var other = new TB_FS_TEMPLATE { ID = Guid.NewGuid(), VAHEDCODE = "20" };
        var ancestor = new TB_FS_TEMPLATE { ID = Guid.NewGuid(), VAHEDCODE = "1" };
        _repo.Setup(r => r.GetTemplateForUpdateAsync(other.ID, It.IsAny<CancellationToken>())).ReturnsAsync(other);
        _repo.Setup(r => r.GetTemplateForUpdateAsync(ancestor.ID, It.IsAny<CancellationToken>())).ReturnsAsync(ancestor);

        await Assert.ThrowsAsync<NotFoundException>(() => Delete().Handle(new DeleteFsTemplateCommand(other.ID) { VahedCode = "10" }, default));
        await Assert.ThrowsAsync<FsAccessDeniedException>(() => Delete().Handle(new DeleteFsTemplateCommand(ancestor.ID) { VahedCode = "10" }, default));
    }

    [Fact]
    public async Task DeleteTemplate_WithActiveVersion_Conflicts()
    {
        var t = new TB_FS_TEMPLATE { ID = Guid.NewGuid(), VAHEDCODE = "10" };
        _repo.Setup(r => r.GetTemplateForUpdateAsync(t.ID, It.IsAny<CancellationToken>())).ReturnsAsync(t);
        _repo.Setup(r => r.GetVersionsForUpdateAsync(t.ID, It.IsAny<CancellationToken>())).ReturnsAsync(new List<TB_FS_TEMPLATE_VERSION>
        {
            new() { STATE = FsTemplateVersionState.Draft }, new() { STATE = FsTemplateVersionState.Active },
        });

        await Assert.ThrowsAsync<FsTemplateConflictException>(() => Delete().Handle(new DeleteFsTemplateCommand(t.ID) { VahedCode = "10" }, default));
        Assert.False(t.ISDELETED);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteTemplate_DraftAndRetiredOnly_SoftDeletesAll()
    {
        var t = new TB_FS_TEMPLATE { ID = Guid.NewGuid(), VAHEDCODE = "10" };
        var versions = new List<TB_FS_TEMPLATE_VERSION>
        {
            new() { STATE = FsTemplateVersionState.Draft }, new() { STATE = FsTemplateVersionState.Retired },
        };
        _repo.Setup(r => r.GetTemplateForUpdateAsync(t.ID, It.IsAny<CancellationToken>())).ReturnsAsync(t);
        _repo.Setup(r => r.GetVersionsForUpdateAsync(t.ID, It.IsAny<CancellationToken>())).ReturnsAsync(versions);

        await Delete().Handle(new DeleteFsTemplateCommand(t.ID) { VahedCode = "10" }, default);

        Assert.True(t.ISDELETED);
        Assert.All(versions, v => { Assert.True(v.ISDELETED); Assert.Equal("editor", v.CHANGEUSERID); });
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ───────── Delete version ─────────

    private static TB_FS_TEMPLATE_VERSION Version(FsTemplateVersionState state, string? owner = "10", string addUser = "creator")
    {
        var t = new TB_FS_TEMPLATE { ID = Guid.NewGuid(), VAHEDCODE = owner, CODE = "OWN.T" };
        return new TB_FS_TEMPLATE_VERSION { ID = Guid.NewGuid(), TEMPLATE_ID = t.ID, TEMPLATE = t, STATE = state, ADDUSERID = addUser, VERSION_NO = 2 };
    }

    private TB_FS_TEMPLATE_VERSION Setup(TB_FS_TEMPLATE_VERSION v, params TB_FS_TEMPLATE_ROW[] rows)
    {
        _repo.Setup(r => r.GetVersionForUpdateAsync(v.ID, It.IsAny<CancellationToken>())).ReturnsAsync(v);
        _repo.Setup(r => r.GetRowsForUpdateAsync(v.ID, It.IsAny<CancellationToken>())).ReturnsAsync(rows.ToList());
        return v;
    }

    [Fact]
    public async Task DeleteVersion_Draft_SoftDeletes()
    {
        var v = Setup(Version(FsTemplateVersionState.Draft));

        await new DeleteFsTemplateVersionCommandHandler(_repo.Object, _scopes.Object, _uow.Object, _user.Object)
            .Handle(new DeleteFsTemplateVersionCommand(v.ID) { VahedCode = "10" }, default);

        Assert.True(v.ISDELETED);
        Assert.Equal("editor", v.CHANGEUSERID);
    }

    [Theory]
    [InlineData(FsTemplateVersionState.Active)]
    [InlineData(FsTemplateVersionState.Retired)]
    public async Task DeleteVersion_NonDraft_Conflicts(FsTemplateVersionState state)
    {
        var v = Setup(Version(state));

        await Assert.ThrowsAsync<FsTemplateConflictException>(() =>
            new DeleteFsTemplateVersionCommandHandler(_repo.Object, _scopes.Object, _uow.Object, _user.Object)
                .Handle(new DeleteFsTemplateVersionCommand(v.ID) { VahedCode = "10" }, default));
        Assert.False(v.ISDELETED);
    }

    // ───────── Add row ─────────

    private AddFsTemplateRowCommandHandler AddRow() => new(_repo.Object, _scopes.Object, _uow.Object, _user.Object);

    private static AddFsTemplateRowCommand NewRow(Guid versionId, string code = "A01", string? parent = null, int? order = null)
        => new(versionId, new FsTemplateRowInput(code, FsRowType.Account, "حساب", ParentCode: parent, NormalBalance: FsNormalBalance.Debit, Selector: "13*", ValueType: FsValueType.Closing, OrderNo: order)) { VahedCode = "10" };

    [Fact]
    public async Task AddRow_FirstRow_GetsOrder10()
    {
        var v = Setup(Version(FsTemplateVersionState.Draft));
        List<TB_FS_TEMPLATE_ROW>? added = null;
        _repo.Setup(r => r.AddRowsAsync(It.IsAny<IEnumerable<TB_FS_TEMPLATE_ROW>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<TB_FS_TEMPLATE_ROW>, CancellationToken>((rows, _) => added = rows.ToList());

        var id = await AddRow().Handle(NewRow(v.ID), default);

        var row = Assert.Single(added!);
        Assert.Equal(id, row.ID);
        Assert.Equal(10, row.ORDER_NO);
        Assert.Equal("editor", row.ADDUSERID);
    }

    [Fact]
    public async Task AddRow_AppendsAfterMaxOrder_AndResolvesParent()
    {
        var header = new TB_FS_TEMPLATE_ROW { ID = Guid.NewGuid(), CODE = "H", ROW_TYPE = FsRowType.Header, ORDER_NO = 40 };
        var v = Setup(Version(FsTemplateVersionState.Draft), header, new TB_FS_TEMPLATE_ROW { ID = Guid.NewGuid(), CODE = "A00", ROW_TYPE = FsRowType.External, ORDER_NO = 25 });
        List<TB_FS_TEMPLATE_ROW>? added = null;
        _repo.Setup(r => r.AddRowsAsync(It.IsAny<IEnumerable<TB_FS_TEMPLATE_ROW>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<TB_FS_TEMPLATE_ROW>, CancellationToken>((rows, _) => added = rows.ToList());

        await AddRow().Handle(NewRow(v.ID, parent: "H"), default);

        Assert.Equal(50, added!.Single().ORDER_NO);
        Assert.Equal(header.ID, added.Single().PARENT_ID);
    }

    [Fact]
    public async Task AddRow_DuplicateCode_Conflicts()
    {
        var v = Setup(Version(FsTemplateVersionState.Draft), new TB_FS_TEMPLATE_ROW { ID = Guid.NewGuid(), CODE = "A01", ROW_TYPE = FsRowType.External, ORDER_NO = 10 });

        await Assert.ThrowsAsync<FsTemplateConflictException>(() => AddRow().Handle(NewRow(v.ID), default));
    }

    [Fact]
    public async Task AddRow_ToActiveVersion_Conflicts()
    {
        var v = Setup(Version(FsTemplateVersionState.Active));

        await Assert.ThrowsAsync<FsTemplateConflictException>(() => AddRow().Handle(NewRow(v.ID), default));
    }

    [Fact]
    public async Task AddRow_UnknownVersion_NotFound()
    {
        _repo.Setup(r => r.GetVersionForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((TB_FS_TEMPLATE_VERSION?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => AddRow().Handle(NewRow(Guid.NewGuid()), default));
    }

    // ───────── Activate ─────────

    private ActivateFsTemplateVersionCommandHandler Activate() => new(_repo.Object, _scopes.Object, _read.Object, _uow.Object, _user.Object);

    private static TB_FS_TEMPLATE_ROW ValidAccountRow(string addUser = "creator")
        => new()
        {
            ID = Guid.NewGuid(), CODE = "A01", ROW_TYPE = FsRowType.Account, ORDER_NO = 10, TITLE_FA = "حساب",
            SELECTOR = "13*", VALUE_TYPE = FsValueType.Closing, NORMAL_BALANCE = FsNormalBalance.Debit, ADDUSERID = addUser,
        };

    private static ActivateFsTemplateVersionCommand Act(Guid id, int year = 1404) => new(id, year) { VahedCode = "10" };

    [Fact]
    public async Task Activate_Valid_ActivatesStampsHash_AndRetiresSiblingWithSameEffectiveYear()
    {
        var v = Setup(Version(FsTemplateVersionState.Draft), ValidAccountRow());
        var sameYear = new TB_FS_TEMPLATE_VERSION { ID = Guid.NewGuid(), STATE = FsTemplateVersionState.Active, EFFECTIVE_FROM_YEAR = 1404 };
        var otherYear = new TB_FS_TEMPLATE_VERSION { ID = Guid.NewGuid(), STATE = FsTemplateVersionState.Active, EFFECTIVE_FROM_YEAR = 1400 };
        _repo.Setup(r => r.GetVersionsForUpdateAsync(v.TEMPLATE_ID, It.IsAny<CancellationToken>())).ReturnsAsync(new List<TB_FS_TEMPLATE_VERSION> { v, sameYear, otherYear });

        await Activate().Handle(Act(v.ID), default);

        Assert.Equal(FsTemplateVersionState.Active, v.STATE);
        Assert.Equal(1404, v.EFFECTIVE_FROM_YEAR);
        Assert.Equal("editor", v.ACTIVATED_BY);
        Assert.Equal(64, v.CONTENT_HASH!.Length);
        Assert.Equal(FsTemplateVersionState.Retired, sameYear.STATE);
        Assert.Equal(FsTemplateVersionState.Active, otherYear.STATE);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Activate_ByTheDraftCreator_IsForbidden_SegregationOfDuties()
    {
        var v = Setup(Version(FsTemplateVersionState.Draft, addUser: "editor"), ValidAccountRow());

        await Assert.ThrowsAsync<FsAccessDeniedException>(() => Activate().Handle(Act(v.ID), default));
        Assert.Equal(FsTemplateVersionState.Draft, v.STATE);
    }

    [Fact]
    public async Task Activate_ByEditorOfAnyRow_IsForbidden_CaseInsensitive()
    {
        var row = ValidAccountRow();
        row.CHANGEUSERID = "EDITOR";
        var v = Setup(Version(FsTemplateVersionState.Draft), row);

        await Assert.ThrowsAsync<FsAccessDeniedException>(() => Activate().Handle(Act(v.ID), default));
    }

    [Fact]
    public async Task Activate_NonDraft_Conflicts()
    {
        var v = Setup(Version(FsTemplateVersionState.Active), ValidAccountRow());

        await Assert.ThrowsAsync<FsTemplateConflictException>(() => Activate().Handle(Act(v.ID), default));
    }

    [Fact]
    public async Task Activate_TemplateWithErrors_ThrowsValidationException_AndDoesNotSave()
    {
        var bad = ValidAccountRow();
        bad.SELECTOR = null;
        var v = Setup(Version(FsTemplateVersionState.Draft), bad);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => Activate().Handle(Act(v.ID), default));

        Assert.Contains(ex.Errors, e => e.PropertyName == "A01.Selector");
        Assert.Equal(FsTemplateVersionState.Draft, v.STATE);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Activate_WarningsOnly_DoNotBlock()
    {
        var row = ValidAccountRow();
        row.TITLE_FA = null;      // فقط هشدار
        var v = Setup(Version(FsTemplateVersionState.Draft), row);

        await Activate().Handle(Act(v.ID), default);

        Assert.Equal(FsTemplateVersionState.Active, v.STATE);
    }

    [Fact]
    public async Task Activate_OfAncestorsTemplate_IsForbidden_BeforeAnyChange()
    {
        var v = Setup(Version(FsTemplateVersionState.Draft, owner: "1"), ValidAccountRow());

        await Assert.ThrowsAsync<FsAccessDeniedException>(() => Activate().Handle(Act(v.ID), default));
    }

    [Theory]
    [InlineData(1299, false)]
    [InlineData(1300, true)]
    [InlineData(1600, true)]
    [InlineData(1601, false)]
    public void ActivateValidator_YearRange(int year, bool valid)
    {
        Assert.Equal(valid, new ActivateFsTemplateVersionCommandValidator().Validate(new ActivateFsTemplateVersionCommand(Guid.NewGuid(), year)).IsValid);
    }
}

public sealed class FsTemplateRowInputValidatorTests
{
    private readonly FsTemplateRowInputValidator _sut = new();

    private static FsTemplateRowInput Account(string code = "A01", string? selector = "13*", FsValueType? vt = FsValueType.Closing, FsNormalBalance? nb = FsNormalBalance.Debit)
        => new(code, FsRowType.Account, "حساب", Selector: selector, ValueType: vt, NormalBalance: nb);

    [Fact]
    public void ValidAccountRow_Passes() => Assert.True(_sut.Validate(Account()).IsValid);

    [Fact]
    public void HeaderRow_NeedsNothingElse() => Assert.True(_sut.Validate(new FsTemplateRowInput("H", FsRowType.Header, "گروه")).IsValid);

    [Theory]
    [InlineData("SUM")]
    [InlineData("1A")]
    [InlineData("A-1")]
    [InlineData("")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTU")]   // ۲۱ نویسه
    public void InvalidRowCode_Fails(string code) => Assert.False(_sut.Validate(Account(code)).IsValid);

    [Fact]
    public void ParentEqualToSelf_Fails()
        => Assert.False(_sut.Validate(new FsTemplateRowInput("H", FsRowType.Header, "گروه", ParentCode: "H")).IsValid);

    [Fact]
    public void AccountRow_MissingPieces_Fail()
    {
        Assert.False(_sut.Validate(Account(selector: null)).IsValid);
        Assert.False(_sut.Validate(Account(selector: "13[X]")).IsValid);
        Assert.False(_sut.Validate(Account(vt: null)).IsValid);
        Assert.False(_sut.Validate(Account(nb: null)).IsValid);
    }

    [Fact]
    public void FormulaRow_NeedsParsableFormulaAndNormalBalance()
    {
        FsTemplateRowInput F(string? formula, FsNormalBalance? nb = FsNormalBalance.Debit) => new("T", FsRowType.Formula, "ج", NormalBalance: nb, Formula: formula);

        Assert.True(_sut.Validate(F("A01 + 1")).IsValid);
        Assert.False(_sut.Validate(F(null)).IsValid);
        Assert.False(_sut.Validate(F("A01 +")).IsValid);
        Assert.False(_sut.Validate(F("A01", nb: null)).IsValid);
    }

    [Fact]
    public void ExternalRow_NeedsNormalBalance()
    {
        Assert.False(_sut.Validate(new FsTemplateRowInput("E", FsRowType.External, "x")).IsValid);
        Assert.True(_sut.Validate(new FsTemplateRowInput("E", FsRowType.External, "x", NormalBalance: FsNormalBalance.Credit)).IsValid);
    }

    [Fact]
    public void FormatIndent_MustBe0To5()
    {
        FsTemplateRowInput H(int indent) => new("H", FsRowType.Header, "گروه", Format: new FsRowFormat(Indent: indent));

        Assert.True(_sut.Validate(H(5)).IsValid);
        Assert.False(_sut.Validate(H(6)).IsValid);
        Assert.False(_sut.Validate(H(-1)).IsValid);
    }

    [Fact]
    public void FieldLengths_AreLimited()
    {
        Assert.False(_sut.Validate(new FsTemplateRowInput("H", FsRowType.Header, new string('x', 501))).IsValid);
        Assert.False(_sut.Validate(new FsTemplateRowInput("H", FsRowType.Header, "x", NoteRef: new string('1', 21))).IsValid);
        Assert.False(_sut.Validate(new FsTemplateRowInput("H", FsRowType.Header, "x", OrderNo: 100000)).IsValid);
    }
}
