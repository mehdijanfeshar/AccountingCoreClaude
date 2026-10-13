using Accounting.Application.OperationTemplates;
using Accounting.Domain.OperationTemplates;

namespace Accounting.Application.Tests.OperationTemplates;

/// <summary>سازنده‌های کمکی الگو و معین برای تست‌های حسابیار.</summary>
internal static class TemplateBuilders
{
    public static readonly Guid Acc1 = Guid.NewGuid();
    public static readonly Guid Acc2 = Guid.NewGuid();
    public static readonly Guid Group1 = Guid.NewGuid();
    public static readonly Guid LevelId1 = Guid.NewGuid();

    public static TemplateParameter Amount(string key = "amount", bool required = true, int sort = 1) => new()
    {
        Id = Guid.NewGuid(), Key = key, Title = "مبلغ " + key, Type = ParameterType.Amount, IsRequired = required,
        AskPrompt = "مبلغ چقدر است؟", SortOrder = sort,
    };

    public static TemplateParameter DetailParam(string key = "person", Guid? group = null, bool required = true, int sort = 2) => new()
    {
        Id = Guid.NewGuid(), Key = key, Title = "شخص", Type = ParameterType.Detail, DetailGroupId = group ?? Group1,
        IsRequired = required, AskPrompt = "کدام شخص؟", SortOrder = sort,
    };

    public static TemplateParameter TextParam(string key = "note1", int sort = 3) => new()
    {
        Id = Guid.NewGuid(), Key = key, Title = "توضیح", Type = ParameterType.Text, IsRequired = false,
        AskPrompt = "توضیح؟", SortOrder = sort,
    };

    public static TemplateLine Line(LineSide side, Guid acc, string? amountKey = "amount", decimal percent = 100m,
        bool balancing = false, int sort = 1, params TemplateLineDetail[] details) => new()
    {
        Id = Guid.NewGuid(), Side = side, SubsidiaryAccountId = acc, AmountParameterKey = balancing ? "" : amountKey!,
        Percent = percent, IsBalancingLine = balancing, SortOrder = sort, Details = details.ToList(),
    };

    public static TemplateLineDetail ParamDetail(int level, string key) => new() { Id = Guid.NewGuid(), Level = level, ParameterKey = key };

    public static TemplateLineDetail FixedDetail(int level, Guid id) => new() { Id = Guid.NewGuid(), Level = level, FixedDetailId = id };

    /// <summary>الگوی ساده و معتبر: بدهکار Acc1 / بستانکار Acc2 هر دو ۱۰۰٪ مبلغ.</summary>
    public static OperationTemplate Simple() => new()
    {
        Id = Guid.NewGuid(), Code = "RECEIVE_CASH", Title = "دریافت وجه", Description = "توضیح",
        VoucherDescriptionPattern = "دریافت {amount}", IsActive = true, CreatedBy = "u1",
        Parameters = [Amount()],
        Lines = [Line(LineSide.Debit, Acc1, sort: 1), Line(LineSide.Credit, Acc2, sort: 2)],
    };

    public static SubsidiaryAccountInfo Account(Guid id, string title = "حساب", bool active = true, params DetailLevelRule[] levels)
        => new(id, "110101", title, active, levels);

    public static DetailLevelRule Level(int level, bool required = false, params Guid[] groups)
        => new(level, LevelId1, groups.ToHashSet(), required);
}
