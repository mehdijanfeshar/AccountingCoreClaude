using System.Globalization;
using System.Text.Json;
using Accounting.Application.OperationTemplates;
using Accounting.Domain.Accrual;
using Accounting.Domain.OperationTemplates;

namespace Accounting.Application.Accrual;

/// <summary>سطرهای زمان‌بندی یک اجرای الگو، پیش از معلوم شدن شناسهٔ سند.</summary>
public sealed record AccrualPlan(IReadOnlyList<AccrualPlanLine> Lines, AccrualTafsiliSides? Sides, Guid? DebitAccountId, Guid? CreditAccountId);

public sealed record AccrualPlanLine(AccrualScheduleKind Kind, int LineNo, string DueDate, long Amount, string Description);

/// <summary>
/// هنگام اجرای الگوی حسابیار (<c>ExecuteOperationHandler</c>) سطرهای <c>TB_AC_SCHEDULE</c> را می‌سازد تا در همان
/// SaveChanges سند ذخیره شوند:
/// <list type="bullet">
/// <item><see cref="OperationTemplate.AutoReverse"/> ⇒ یک سطر برگشت با سررسید روز اول ماه بعد؛ برآورد اسفند برگشت نمی‌گیرد
/// (تصمیم صاحب پروژه ۲۰۲۶-۱۰-۳۰: برگشت در سال جدید یا داخل اسفند اثر برآورد پایان سال را از بین می‌برد).</item>
/// <item><see cref="OperationTemplate.ScheduleKind"/> ⇒ N سطر سهم ماهانه (N = پارامتر <c>months</c>) از ماه تاریخ سند، سررسید
/// آخر هر ماه؛ حساب و تفصیلی دو طرف از ردیف‌های «سهم ماهانه» الگو با همان موتور ساخته می‌شوند.</item>
/// </list>
/// </summary>
public sealed class AccrualSchedulePlanner
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly IVoucherGenerationEngine _engine;

    public AccrualSchedulePlanner(IVoucherGenerationEngine engine) => _engine = engine;

    public static bool IsScheduleLine(TemplateLine line) =>
        string.Equals(line.ConditionParamKey, AccrualConventions.ScheduleLineKey, StringComparison.Ordinal);

    public async Task<(AccrualPlan? Plan, EngineError? Error)> PlanAsync(
        OperationTemplate template, string vahedCode, DateOnly date, IReadOnlyDictionary<string, string?> values,
        VoucherDraft mainDraft, CancellationToken ct)
    {
        var jalali = AccrualDates.ToJalali(date);
        var year = int.Parse(jalali[..4], CultureInfo.InvariantCulture);
        var month = int.Parse(jalali[4..6], CultureInfo.InvariantCulture);

        if (template.AutoReverse)
        {
            if (month == 12) return (null, null);
            var line = new AccrualPlanLine(AccrualScheduleKind.Reversal, 1, AccrualDates.FirstDayOfNextMonth(year, month),
                mainDraft.TotalDebit, Truncate($"برگشت خودکار برآورد: {mainDraft.Description}"));
            return (new AccrualPlan([line], null, null, null), null);
        }

        if (template.ScheduleKind is not { } kind) return (null, null);

        if (!values.TryGetValue(AccrualConventions.MonthsParameterKey, out var rawMonths)
            || !int.TryParse(rawMonths, NumberStyles.None, CultureInfo.InvariantCulture, out var months)
            || months < 1 || months > AccrualConventions.MaxMonths)
            return (null, new EngineError(EngineErrorCode.InvalidParameterValue,
                $"تعداد ماه تقسیم باید عددی بین ۱ تا {AccrualConventions.MaxMonths} باشد.", AccrualConventions.MonthsParameterKey));

        // ردیف‌های «سهم ماهانه» بدون شرط ⇒ همان موتور حساب، تفصیلی و مبلغ را می‌سازد و کنترل می‌کند.
        var shadow = new OperationTemplate
        {
            Id = template.Id, Code = template.Code, Title = template.Title, Description = template.Description,
            VoucherDescriptionPattern = template.VoucherDescriptionPattern, IsActive = true,
            Parameters = template.Parameters,
            Lines = template.Lines.Where(IsScheduleLine).Select(l => new TemplateLine
            {
                Id = l.Id, OperationTemplateId = l.OperationTemplateId, Side = l.Side, SubsidiaryAccountId = l.SubsidiaryAccountId,
                AmountParameterKey = l.AmountParameterKey, Percent = l.Percent, IsBalancingLine = l.IsBalancingLine,
                DescriptionPattern = l.DescriptionPattern, SortOrder = l.SortOrder, AmountSource = l.AmountSource,
                Details = l.Details,
            }).ToList(),
        };
        var r = await _engine.GenerateAsync(shadow, vahedCode, date, values, ct);
        if (!r.IsSuccess) return (null, r.Errors[0]);

        var debit = r.Draft!.Lines.Single(l => l.Debit > 0);
        var credit = r.Draft.Lines.Single(l => l.Credit > 0);
        var total = r.Draft.TotalDebit;
        if (total < months)
            return (null, new EngineError(EngineErrorCode.NonPositiveAmount, "مبلغ برای تقسیم روی این تعداد ماه کم است."));

        var shares = AccrualScheduleSplitter.Split(total, months);
        var title = kind == AccrualScheduleKind.Prepaid ? "سهم ماهانهٔ پیش‌پرداخت" : "سهم ماهانهٔ درآمد انتقالی";
        var lines = shares.Select((amount, i) => new AccrualPlanLine(kind, i + 1, AccrualDates.LastDayOfMonth(year, month, i), amount,
            Truncate($"{title} ({i + 1} از {months}): {mainDraft.Description}"))).ToList();
        var sides = new AccrualTafsiliSides(
            debit.Details.Select(d => new AccrualTafsili(d.Level, d.LevelId, d.DetailId, d.DetailTitle)).ToList(),
            credit.Details.Select(d => new AccrualTafsili(d.Level, d.LevelId, d.DetailId, d.DetailTitle)).ToList());
        return (new AccrualPlan(lines, sides, debit.SubsidiaryAccountId, credit.SubsidiaryAccountId), null);
    }

    public static IReadOnlyList<AccrualSchedule> Stage(AccrualPlan plan, string vahedCode, Guid voucherId, Guid? executionId, string userId, DateTime nowUtc)
    {
        var json = plan.Sides is null ? null : JsonSerializer.Serialize(plan.Sides, Json);
        return plan.Lines.Select(l => new AccrualSchedule
        {
            Id = Guid.NewGuid(),
            VahedCode = vahedCode,
            Year = l.DueDate[..4],
            SourceVoucherId = voucherId,
            ExecutionId = executionId,
            Kind = l.Kind,
            LineNo = l.LineNo,
            DueDate = l.DueDate,
            Amount = l.Amount,
            DebitAccountId = plan.DebitAccountId,
            CreditAccountId = plan.CreditAccountId,
            TafsiliJson = json,
            Description = l.Description,
            State = AccrualScheduleState.Open,
            CreatedBy = userId,
            CreatedAtUtc = nowUtc,
        }).ToList();
    }

    public static AccrualTafsiliSides ReadSides(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? new AccrualTafsiliSides([], [])
            : JsonSerializer.Deserialize<AccrualTafsiliSides>(json, Json) ?? new AccrualTafsiliSides([], []);

    private static string Truncate(string s) => s.Length <= 500 ? s : s[..500];
}

/// <summary>قواعد ذخیرهٔ الگوی تعهدی؛ از <see cref="TemplateDefinitionValidator"/> صدا زده می‌شود.</summary>
public static class AccrualTemplateRules
{
    public static void Validate(OperationTemplate t, IReadOnlyDictionary<string, TemplateParameter> pByKey, List<string> e)
    {
        var scheduleLines = t.Lines.Where(AccrualSchedulePlanner.IsScheduleLine).ToList();

        if (t.AutoReverse && t.ScheduleKind is not null)
            e.Add("«برگشت خودکار اول ماه بعد» و «تقسیم روی چند ماه» با هم ممکن نیست؛ یکی را انتخاب کنید.");

        if (t.ScheduleKind is null)
        {
            if (scheduleLines.Count > 0)
                e.Add("ردیف «سهم ماهانه» فقط در الگویی مجاز است که «تقسیم روی چند ماه» دارد.");
            return;
        }

        if (t.ScheduleKind is not (AccrualScheduleKind.Prepaid or AccrualScheduleKind.Deferred))
        { e.Add("نوع تقسیم فقط «پیش‌پرداخت» یا «درآمد انتقالی» است."); return; }

        if (!pByKey.TryGetValue(AccrualConventions.MonthsParameterKey, out var mp)
            || mp.Type is not (ParameterType.Amount or ParameterType.Text) || !mp.IsRequired)
            e.Add($"الگوی تقسیم روی چند ماه باید پارامتر الزامی «{AccrualConventions.MonthsParameterKey}» (تعداد ماه) داشته باشد.");

        if (scheduleLines.Count != 2
            || scheduleLines.Count(l => l.Side == LineSide.Debit) != 1
            || scheduleLines.Count(l => l.Side == LineSide.Credit) != 1)
        {
            e.Add("الگوی تقسیم روی چند ماه باید دقیقاً دو ردیف «سهم ماهانه» داشته باشد: یک بدهکار و یک بستانکار.");
            return;
        }

        if (scheduleLines.Any(l => l.IsBalancingLine || l.Percent != 100m || l.AmountSource != LineAmountSource.Parameter)
            || !string.Equals(scheduleLines[0].AmountParameterKey, scheduleLines[1].AmountParameterKey, StringComparison.OrdinalIgnoreCase))
            e.Add("دو ردیف «سهم ماهانه» باید هر دو ۱۰۰٪ از یک پارامتر مبلغ باشند و ردیف تراز‌کننده نباشند.");
    }
}
