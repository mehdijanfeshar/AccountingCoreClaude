using Accounting.Application.Commands.FinancialStatements.Common;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.FinancialStatements;
using Accounting.Application.FinancialStatements.Engine;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using FluentValidation;

namespace Accounting.Application.Tests.FinancialStatements.Commands;

/// <summary>قواعد مشترک Commandهای قالب: تغییرناپذیری نسخهٔ غیرپیش‌نویس، مالکیت، والد/دور، hash محتوا.</summary>
public sealed class FsTemplateRulesTests
{
    private static TB_FS_TEMPLATE_ROW Row(string code, FsRowType type = FsRowType.Account, Guid? parentId = null, int order = 10, string? formula = null)
        => new()
        {
            ID = Guid.NewGuid(), CODE = code, ROW_TYPE = type, PARENT_ID = parentId, ORDER_NO = order, TITLE_FA = "عنوان " + code,
            NORMAL_BALANCE = FsNormalBalance.Debit, SELECTOR = type == FsRowType.Account ? "13*" : null,
            VALUE_TYPE = type == FsRowType.Account ? FsValueType.Closing : null, FORMULA = formula, IS_DRILLABLE = true,
        };

    // ───────── EnsureDraft ─────────

    [Fact]
    public void EnsureDraft_Draft_DoesNotThrow()
    {
        FsTemplateRules.EnsureDraft(new TB_FS_TEMPLATE_VERSION { STATE = FsTemplateVersionState.Draft });
    }

    [Theory]
    [InlineData(FsTemplateVersionState.Active, "فعال")]
    [InlineData(FsTemplateVersionState.Retired, "بازنشسته")]
    public void EnsureDraft_NonDraft_ThrowsConflictNamingState(FsTemplateVersionState state, string label)
    {
        var ex = Assert.Throws<FsTemplateConflictException>(() =>
            FsTemplateRules.EnsureDraft(new TB_FS_TEMPLATE_VERSION { STATE = state, VERSION_NO = 3 }));

        Assert.Contains(label, ex.Message);
        Assert.Contains("3", ex.Message);
    }

    // ───────── EnsureEditable ─────────

    [Fact]
    public void EnsureEditable_UnrelatedUnitsTemplate_IsNotFound_NotForbidden()
    {
        var t = new TB_FS_TEMPLATE { ID = Guid.NewGuid(), VAHEDCODE = "20" };

        Assert.Throws<NotFoundException>(() => FsTemplateRules.EnsureEditable(FsTestData.ProvinceScope(), t));
    }

    [Fact]
    public void EnsureEditable_AncestorOrSharedTemplate_IsForbidden()
    {
        Assert.Throws<FsAccessDeniedException>(() => FsTemplateRules.EnsureEditable(FsTestData.ProvinceScope(), new TB_FS_TEMPLATE { VAHEDCODE = "1" }));
        Assert.Throws<FsAccessDeniedException>(() => FsTemplateRules.EnsureEditable(FsTestData.ProvinceScope(), new TB_FS_TEMPLATE { VAHEDCODE = null }));
    }

    [Fact]
    public void EnsureEditable_OwnDescendantAndHqShared_Pass()
    {
        FsTemplateRules.EnsureEditable(FsTestData.ProvinceScope(), new TB_FS_TEMPLATE { VAHEDCODE = "10" });
        FsTemplateRules.EnsureEditable(FsTestData.ProvinceScope(), new TB_FS_TEMPLATE { VAHEDCODE = "101" });
        FsTemplateRules.EnsureEditable(FsTestData.HqScope(), new TB_FS_TEMPLATE { VAHEDCODE = null });
    }

    // ───────── ApplyNoteLink ─────────

    [Fact]
    public void ApplyNoteLink_NoteTemplate_StoresTrimmedValues()
    {
        var t = new TB_FS_TEMPLATE { STATEMENT_TYPE = FsStatementType.Note };

        FsTemplateRules.ApplyNoteLink(t, " P.NET ", " A01 ", " T01 ");

        Assert.Equal("P.NET", t.NOTE_PARENT_TEMPLATE_CODE);
        Assert.Equal("A01", t.NOTE_PARENT_ROW_CODE);
        Assert.Equal("T01", t.NOTE_TOTAL_ROW_CODE);
    }

    [Fact]
    public void ApplyNoteLink_NonNoteTemplate_ClearsLinks()
    {
        var t = new TB_FS_TEMPLATE
        {
            STATEMENT_TYPE = FsStatementType.NetAssets,
            NOTE_PARENT_TEMPLATE_CODE = "old", NOTE_PARENT_ROW_CODE = "old", NOTE_TOTAL_ROW_CODE = "old",
        };

        FsTemplateRules.ApplyNoteLink(t, "P", "A", "T");

        Assert.Null(t.NOTE_PARENT_TEMPLATE_CODE);
        Assert.Null(t.NOTE_PARENT_ROW_CODE);
        Assert.Null(t.NOTE_TOTAL_ROW_CODE);
    }

    [Fact]
    public void ApplyNoteLink_BlankValuesBecomeNull()
    {
        var t = new TB_FS_TEMPLATE { STATEMENT_TYPE = FsStatementType.Note };

        FsTemplateRules.ApplyNoteLink(t, "  ", "", null);

        Assert.Null(t.NOTE_PARENT_TEMPLATE_CODE);
        Assert.Null(t.NOTE_PARENT_ROW_CODE);
        Assert.Null(t.NOTE_TOTAL_ROW_CODE);
    }

    // ───────── ResolveParent ─────────

    [Fact]
    public void ResolveParent_Null_ReturnsNull()
    {
        Assert.Null(FsTemplateRules.ResolveParent(null, Row("X"), []));
    }

    [Fact]
    public void ResolveParent_ExistingHeader_ReturnsItsId()
    {
        var h = Row("H", FsRowType.Header);
        var self = Row("X");

        Assert.Equal(h.ID, FsTemplateRules.ResolveParent("H", self, [h, self]));
    }

    [Fact]
    public void ResolveParent_Missing_ThrowsValidationOnParentCode()
    {
        var ex = Assert.Throws<ValidationException>(() => FsTemplateRules.ResolveParent("NOPE", Row("X"), []));

        Assert.Equal("ParentCode", ex.Errors.Single().PropertyName);
    }

    [Fact]
    public void ResolveParent_NotAHeader_Throws()
    {
        var notHeader = Row("A", FsRowType.Account);

        Assert.Throws<ValidationException>(() => FsTemplateRules.ResolveParent("A", Row("X"), [notHeader]));
    }

    [Fact]
    public void ResolveParent_SelfAsParent_IsMissing()
    {
        var self = Row("H", FsRowType.Header);

        Assert.Throws<ValidationException>(() => FsTemplateRules.ResolveParent("H", self, [self]));
    }

    [Fact]
    public void ResolveParent_WouldCreateCycle_Throws()
    {
        // B زیر A است؛ حالا A را زیر B بگذاریم → دور.
        var a = Row("A", FsRowType.Header);
        var b = Row("B", FsRowType.Header, parentId: a.ID);

        var ex = Assert.Throws<ValidationException>(() => FsTemplateRules.ResolveParent("B", a, [a, b]));

        Assert.Contains("دور", ex.Errors.Single().ErrorMessage);
    }

    [Fact]
    public void ResolveParent_DeepChainWithoutCycle_Passes()
    {
        var a = Row("A", FsRowType.Header);
        var b = Row("B", FsRowType.Header, parentId: a.ID);
        var self = Row("X");

        Assert.Equal(b.ID, FsTemplateRules.ResolveParent("B", self, [a, b, self]));
    }

    // ───────── BuildRows ─────────

    [Fact]
    public void BuildRows_DuplicateCode_Throws()
    {
        var inputs = new[] { new FsTemplateRowInput("A1", FsRowType.Header, "ت"), new FsTemplateRowInput("A1", FsRowType.Header, "ت") };

        var ex = Assert.Throws<ValidationException>(() => FsTemplateRules.BuildRows(Guid.NewGuid(), inputs, "u", DateTime.UtcNow));

        Assert.Equal("Rows", ex.Errors.Single().PropertyName);
    }

    [Fact]
    public void BuildRows_AssignsOrderParentsAndStamps()
    {
        var versionId = Guid.NewGuid();
        var inputs = new[]
        {
            new FsTemplateRowInput("H", FsRowType.Header, "گروه"),
            new FsTemplateRowInput("A1", FsRowType.Account, " حساب ", ParentCode: "H", NormalBalance: FsNormalBalance.Debit, Selector: "۱۳*  !1305", ValueType: FsValueType.Closing),
            new FsTemplateRowInput("F1", FsRowType.Formula, "جمع", NormalBalance: FsNormalBalance.Debit, Formula: " A1 * 2 ", OrderNo: 5),
        };

        var rows = FsTemplateRules.BuildRows(versionId, inputs, "creator", new DateTime(2026, 1, 1));

        Assert.Equal(3, rows.Count);
        Assert.All(rows, r => { Assert.Equal(versionId, r.VERSION_ID); Assert.Equal("creator", r.ADDUSERID); });
        Assert.Equal(new[] { 10, 20, 5 }, rows.Select(r => r.ORDER_NO).ToArray());
        Assert.Equal(rows[0].ID, rows[1].PARENT_ID);
        Assert.Null(rows[0].PARENT_ID);
        Assert.Equal("13* !1305", rows[1].SELECTOR);       // نرمال‌سازی ارقام و فاصله
        Assert.Equal("حساب", rows[1].TITLE_FA);
        Assert.Equal("A1 * 2", rows[2].FORMULA);
        Assert.Null(rows[2].SELECTOR);                     // ردیف فرمول انتخاب‌گر ندارد
        Assert.Null(rows[2].VALUE_TYPE);
    }

    [Fact]
    public void BuildRows_ParentDefinedLaterInList_StillResolves()
    {
        var inputs = new[]
        {
            new FsTemplateRowInput("A1", FsRowType.External, "x", ParentCode: "H", NormalBalance: FsNormalBalance.Debit),
            new FsTemplateRowInput("H", FsRowType.Header, "گروه"),
        };

        var rows = FsTemplateRules.BuildRows(Guid.NewGuid(), inputs, "u", DateTime.UtcNow);

        Assert.Equal(rows[1].ID, rows[0].PARENT_ID);
    }

    [Fact]
    public void BuildRows_UnknownParent_Throws()
    {
        var inputs = new[] { new FsTemplateRowInput("A1", FsRowType.External, "x", ParentCode: "H", NormalBalance: FsNormalBalance.Debit) };

        Assert.Throws<ValidationException>(() => FsTemplateRules.BuildRows(Guid.NewGuid(), inputs, "u", DateTime.UtcNow));
    }

    [Fact]
    public void ApplyInput_StoresFormat_OnlyWhenNonDefault()
    {
        var row = new TB_FS_TEMPLATE_ROW();

        FsTemplateRules.ApplyInput(row, new FsTemplateRowInput("H", FsRowType.Header, "t", Format: new FsRowFormat(Bold: true)));
        Assert.Contains("bold", row.FORMAT_JSON);

        FsTemplateRules.ApplyInput(row, new FsTemplateRowInput("H", FsRowType.Header, "t", Format: new FsRowFormat()));
        Assert.Null(row.FORMAT_JSON);
    }

    // ───────── ComputeContentHash ─────────

    [Fact]
    public void ComputeContentHash_IsDeterministic_LowerHex64()
    {
        var rows = new[] { Row("A1", order: 10), Row("A2", order: 20) };

        var h1 = FsTemplateRules.ComputeContentHash(rows);
        var h2 = FsTemplateRules.ComputeContentHash(rows);

        Assert.Equal(h1, h2);
        Assert.Equal(64, h1.Length);
        Assert.Matches("^[0-9a-f]{64}$", h1);
    }

    [Fact]
    public void ComputeContentHash_IgnoresListOrderAndGuids_ButNotContent()
    {
        var h1 = Row("H", FsRowType.Header, order: 10);
        var a1 = Row("A1", parentId: h1.ID, order: 20);
        var h2 = Row("H", FsRowType.Header, order: 10);      // GUID متفاوت
        var a2 = Row("A1", parentId: h2.ID, order: 20);

        Assert.Equal(FsTemplateRules.ComputeContentHash([h1, a1]), FsTemplateRules.ComputeContentHash([a2, h2]));

        a2.SELECTOR = "14*";
        Assert.NotEqual(FsTemplateRules.ComputeContentHash([h1, a1]), FsTemplateRules.ComputeContentHash([h2, a2]));
    }

    [Fact]
    public void ComputeContentHash_ChangesWith_Formula_Order_Parent()
    {
        var baseRow = Row("F", FsRowType.Formula, order: 10, formula: "A1+1");
        var baseHash = FsTemplateRules.ComputeContentHash([baseRow]);

        var changedFormula = Row("F", FsRowType.Formula, order: 10, formula: "A1+2");
        var changedOrder = Row("F", FsRowType.Formula, order: 11, formula: "A1+1");

        Assert.NotEqual(baseHash, FsTemplateRules.ComputeContentHash([changedFormula]));
        Assert.NotEqual(baseHash, FsTemplateRules.ComputeContentHash([changedOrder]));
    }

    // ───────── ToCheckRows ─────────

    [Fact]
    public void ToCheckRows_ResolvesParentCodes()
    {
        var h = Row("H", FsRowType.Header);
        var a = Row("A1", parentId: h.ID);

        var result = FsTemplateRules.ToCheckRows([h, a]);

        Assert.Null(result.Single(r => r.Code == "H").ParentCode);
        Assert.Equal("H", result.Single(r => r.Code == "A1").ParentCode);
        Assert.Equal("13*", result.Single(r => r.Code == "A1").Selector);
    }

    // ───────── Invalid helper ─────────

    [Fact]
    public void Invalid_BuildsValidationExceptionWithPropertyAndMessage()
    {
        var ex = FsTemplateRules.Invalid("Prop", "پیام");

        var failure = Assert.Single(ex.Errors);
        Assert.Equal("Prop", failure.PropertyName);
        Assert.Equal("پیام", failure.ErrorMessage);
    }
}
