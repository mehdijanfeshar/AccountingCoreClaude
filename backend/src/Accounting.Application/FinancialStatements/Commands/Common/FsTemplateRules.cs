using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.FinancialStatements.Engine;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using FluentValidation;
using FluentValidation.Results;

namespace Accounting.Application.FinancialStatements.Commands.Common;

/// <summary>
/// قواعد مشترک Commandهای قالب صورت مالی — طبق قاعدهٔ ۲ CLAUDE.md، گذارهای وضعیت و تغییرناپذیری
/// نسخهٔ فعال اینجا (Application) اجبار می‌شوند، نه در Entity.
/// </summary>
public static class FsTemplateRules
{
    public static void EnsureDraft(TB_FS_TEMPLATE_VERSION version)
    {
        if (version.STATE != FsTemplateVersionState.Draft)
        {
            throw new FsTemplateConflictException(
                $"نسخهٔ {version.VERSION_NO} در وضعیت «{StateLabel(version.STATE)}» است و قابل تغییر نیست. " +
                "برای تغییر، یک نسخهٔ پیش‌نویس جدید از آن بسازید.");
        }
    }

    /// <summary>
    /// قالب واحد نامرتبط = ۴۰۴ (وجودش لو نمی‌رود)؛ دیدنی ولی غیرقابل‌تغییر (مشترک برای غیرستاد، یا قالب
    /// جد) = ۴۰۳. <c>docs/fs-module.md</c> §۸.
    /// </summary>
    public static void EnsureEditable(FsUnitScope scope, TB_FS_TEMPLATE template)
    {
        if (!scope.CanSee(template.VAHEDCODE))
        {
            throw new NotFoundException("FsTemplate", template.ID);
        }

        scope.EnsureCanEdit(template.VAHEDCODE);
    }

    /// <summary>ارتباط یادداشت با ردیف صورت و ردیف جمع — فقط برای قالب نوع یادداشت؛ برای بقیه پاک می‌شود.</summary>
    public static void ApplyNoteLink(TB_FS_TEMPLATE template, string? parentTemplateCode, string? parentRowCode, string? totalRowCode)
    {
        var isNote = template.STATEMENT_TYPE == FsStatementType.Note;
        template.NOTE_PARENT_TEMPLATE_CODE = isNote ? Trim(parentTemplateCode) : null;
        template.NOTE_PARENT_ROW_CODE = isNote ? Trim(parentRowCode) : null;
        template.NOTE_TOTAL_ROW_CODE = isNote ? Trim(totalRowCode) : null;
    }

    /// <summary>قواعد مشترک اعتبارسنجی سه فیلد ارتباط یادداشت در Create/Update.</summary>
    public static void AddNoteLinkRules<T>(
        AbstractValidator<T> validator,
        System.Linq.Expressions.Expression<Func<T, string?>> parentTemplate,
        System.Linq.Expressions.Expression<Func<T, string?>> parentRow,
        System.Linq.Expressions.Expression<Func<T, string?>> totalRow)
    {
        validator.RuleFor(parentTemplate)
            .Must(c => string.IsNullOrWhiteSpace(c) || FsText.IsValidTemplateCode(c.Trim()))
            .WithMessage("کد قالب صورت والد نامعتبر است.")
            .MaximumLength(50);
        validator.RuleFor(parentRow)
            .Must(c => string.IsNullOrWhiteSpace(c) || FsText.IsValidRowCode(c.Trim()))
            .WithMessage("کد ردیف صورت والد نامعتبر است.");
        validator.RuleFor(totalRow)
            .Must(c => string.IsNullOrWhiteSpace(c) || FsText.IsValidRowCode(c.Trim()))
            .WithMessage("کد ردیف جمع یادداشت نامعتبر است.");
    }

    public static string StateLabel(FsTemplateVersionState state) => state switch
    {
        FsTemplateVersionState.Draft => "پیش‌نویس",
        FsTemplateVersionState.Active => "فعال",
        FsTemplateVersionState.Retired => "بازنشسته",
        _ => "نامشخص",
    };

    public static void ApplyInput(TB_FS_TEMPLATE_ROW row, FsTemplateRowInput input)
    {
        row.CODE = input.Code;
        row.ROW_TYPE = input.RowType;
        row.TITLE_FA = Trim(input.TitleFa);
        row.TITLE_EN = Trim(input.TitleEn);
        row.NOTE_REF = Trim(input.NoteRef);
        row.NORMAL_BALANCE = input.NormalBalance;
        row.SELECTOR = input.RowType == FsRowType.Account ? AccountSelector.Parse(input.Selector).ToString() : null;
        row.VALUE_TYPE = input.RowType == FsRowType.Account ? input.ValueType : null;
        row.FORMULA = input.RowType == FsRowType.Formula ? Trim(input.Formula) : null;
        row.FORMAT_JSON = FsRowFormat.ToJson(input.Format);
        row.IS_DRILLABLE = input.IsDrillable;
        row.ALLOW_MANUAL_ADJUST = input.AllowManualAdjust;
    }

    /// <summary>
    /// والد با کد داده شده را در <paramref name="rows"/> پیدا می‌کند؛ باید «عنوان» باشد و دور نسازد.
    /// خطا = ۴۰۰ (ValidationException روی فیلد <c>ParentCode</c>).
    /// </summary>
    public static Guid? ResolveParent(string? parentCode, TB_FS_TEMPLATE_ROW self, IReadOnlyList<TB_FS_TEMPLATE_ROW> rows)
    {
        if (parentCode is null)
        {
            return null;
        }

        var parent = rows.FirstOrDefault(r => r.CODE == parentCode && r.ID != self.ID)
            ?? throw Invalid("ParentCode", $"ردیف والد «{parentCode}» در این نسخه وجود ندارد.");

        if (parent.ROW_TYPE != FsRowType.Header)
        {
            throw Invalid("ParentCode", $"والد «{parentCode}» باید از نوع «عنوان» باشد.");
        }

        var byId = rows.ToDictionary(r => r.ID);

        for (Guid? p = parent.ID; p is { } id; p = byId.TryGetValue(id, out var r) ? r.PARENT_ID : null)
        {
            if (id == self.ID)
            {
                throw Invalid("ParentCode", $"انتخاب «{parentCode}» به‌عنوان والد، دور در درخت ردیف‌ها می‌سازد.");
            }
        }

        return parent.ID;
    }

    /// <summary>
    /// ردیف‌های یک نسخه را از فهرست ورودی می‌سازد (ورود دسته‌ای و seed). کدها باید یکتا باشند؛ والدها
    /// با کد در همین فهرست حل می‌شوند. ترتیب = <c>OrderNo</c> ورودی یا جایگاه در فهرست × ۱۰.
    /// </summary>
    public static IReadOnlyList<TB_FS_TEMPLATE_ROW> BuildRows(
        Guid versionId, IReadOnlyList<FsTemplateRowInput> inputs, string userId, DateTime now)
    {
        var duplicate = inputs.GroupBy(i => i.Code, StringComparer.Ordinal).FirstOrDefault(g => g.Count() > 1);

        if (duplicate is not null)
        {
            throw Invalid("Rows", $"کد ردیف «{duplicate.Key}» تکراری است.");
        }

        var rows = inputs.Select((input, i) =>
        {
            var row = new TB_FS_TEMPLATE_ROW
            {
                ID = Guid.NewGuid(),
                VERSION_ID = versionId,
                ORDER_NO = input.OrderNo ?? (i + 1) * 10,
                CREATEDDATE = now,
                ADDUSERID = userId,
            };
            ApplyInput(row, input);
            return row;
        }).ToList();

        for (var i = 0; i < rows.Count; i++)
        {
            rows[i].PARENT_ID = ResolveParent(inputs[i].ParentCode, rows[i], rows);
        }

        return rows;
    }

    public static ValidationException Invalid(string property, string message)
        => new(new[] { new ValidationFailure(property, message) });

    public static IReadOnlyList<FsCheckRow> ToCheckRows(IReadOnlyList<TB_FS_TEMPLATE_ROW> rows)
    {
        var codeById = rows.ToDictionary(r => r.ID, r => r.CODE);

        return rows
            .Select(r => new FsCheckRow(
                r.CODE,
                r.PARENT_ID is { } pid && codeById.TryGetValue(pid, out var pc) ? pc : null,
                r.ORDER_NO,
                r.ROW_TYPE,
                r.TITLE_FA,
                r.NORMAL_BALANCE,
                r.SELECTOR,
                r.VALUE_TYPE,
                r.FORMULA))
            .ToList();
    }

    /// <summary>SHA-256 (hex) محتوای معنادار ردیف‌ها به ترتیب ارائه — برای <c>CONTENT_HASH</c> نسخهٔ فعال.</summary>
    public static string ComputeContentHash(IReadOnlyList<TB_FS_TEMPLATE_ROW> rows)
    {
        var codeById = rows.ToDictionary(r => r.ID, r => r.CODE);
        var canonical = rows
            .OrderBy(r => r.ORDER_NO)
            .ThenBy(r => r.CODE, StringComparer.Ordinal)
            .Select(r => new object?[]
            {
                r.CODE,
                r.PARENT_ID is { } pid ? codeById.GetValueOrDefault(pid) : null,
                r.ORDER_NO,
                (int)r.ROW_TYPE,
                r.TITLE_FA,
                r.TITLE_EN,
                r.NOTE_REF,
                (int?)r.NORMAL_BALANCE,
                r.SELECTOR,
                (int?)r.VALUE_TYPE,
                r.FORMULA,
                r.FORMAT_JSON,
                r.IS_DRILLABLE,
                r.ALLOW_MANUAL_ADJUST,
            });

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(canonical)));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
