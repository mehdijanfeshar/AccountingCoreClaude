using System.Globalization;
using Accounting.Application.Accrual;
using Accounting.Application.OperationTemplates;
using Accounting.Domain.Accrual;
using Accounting.Domain.OperationTemplates;
using Moq;

namespace Accounting.Application.Tests.Accrual;

/// <summary>سطرهای زمان‌بندی هنگام اجرای الگوی حسابیار، و قواعد ذخیرهٔ الگوی تعهدی (فاز ۶۶).</summary>
public sealed class AccrualSchedulePlannerTests
{
    private const string Vahed = "0001";
    private readonly Mock<IVoucherGenerationEngine> _engine = new();

    private static DateOnly Jalali(int y, int m, int d) =>
        DateOnly.FromDateTime(new PersianCalendar().ToDateTime(y, m, d, 0, 0, 0, 0));

    private static VoucherDraft Draft(long amount, string description = "هزینهٔ نظافت مهر") => new(Vahed, Jalali(1405, 7, 20), description, "T",
    [
        new VoucherDraftLine(Guid.NewGuid(), "", [], amount, 0, description),
        new VoucherDraftLine(Guid.NewGuid(), "", [], 0, amount, description),
    ]);

    private static OperationTemplate Template(bool autoReverse = false, AccrualScheduleKind? kind = null) => new()
    {
        Id = Guid.NewGuid(), Code = "ACCRUE", Title = "برآورد", Description = "d", VoucherDescriptionPattern = "x",
        AutoReverse = autoReverse, ScheduleKind = kind,
        Parameters =
        [
            new TemplateParameter { Key = "amount", Title = "مبلغ", Type = ParameterType.Amount, AskPrompt = "?", IsRequired = true },
            new TemplateParameter { Key = AccrualConventions.MonthsParameterKey, Title = "ماه", Type = ParameterType.Text, AskPrompt = "?", IsRequired = true },
        ],
        Lines =
        [
            Line(LineSide.Debit), Line(LineSide.Credit),
            Line(LineSide.Debit, schedule: true), Line(LineSide.Credit, schedule: true),
        ],
    };

    private static TemplateLine Line(LineSide side, bool schedule = false) => new()
    {
        Id = Guid.NewGuid(), Side = side, SubsidiaryAccountId = Guid.NewGuid(), AmountParameterKey = "amount", Percent = 100m,
        ConditionParamKey = schedule ? AccrualConventions.ScheduleLineKey : null,
        ConditionValue = schedule ? AccrualConventions.ScheduleLineValue : null,
    };

    private AccrualSchedulePlanner Planner() => new(_engine.Object);

    private static Dictionary<string, string?> Values(string? months = null) => new() { ["amount"] = "1", [AccrualConventions.MonthsParameterKey] = months };

    [Fact]
    public async Task AutoReverse_makes_exactly_one_reversal_on_first_of_next_month()
    {
        var (plan, error) = await Planner().PlanAsync(Template(autoReverse: true), Vahed, Jalali(1405, 7, 20), Values(), Draft(80_000_000), default);

        Assert.Null(error);
        var line = Assert.Single(plan!.Lines);
        Assert.Equal(AccrualScheduleKind.Reversal, line.Kind);
        Assert.Equal("14050801", line.DueDate);
        Assert.Equal(80_000_000, line.Amount);
        Assert.Equal(1, line.LineNo);

        var voucherId = Guid.NewGuid();
        var row = Assert.Single(AccrualSchedulePlanner.Stage(plan, Vahed, voucherId, Guid.NewGuid(), "u1", DateTime.UtcNow));
        Assert.Equal(voucherId, row.SourceVoucherId);
        Assert.Equal(AccrualScheduleState.Open, row.State);
        Assert.Equal("1405", row.Year);
    }

    [Fact]
    public async Task Esfand_estimate_gets_no_automatic_reversal()
    {
        var (plan, error) = await Planner().PlanAsync(Template(autoReverse: true), Vahed, Jalali(1405, 12, 25), Values(), Draft(80_000_000), default);

        Assert.Null(error);
        Assert.Null(plan);
    }

    [Fact]
    public async Task Template_without_accrual_flags_makes_no_rows()
    {
        var (plan, error) = await Planner().PlanAsync(Template(), Vahed, Jalali(1405, 7, 20), Values(), Draft(80_000_000), default);

        Assert.Null(error);
        Assert.Null(plan);
    }

    [Fact]
    public async Task Prepaid_twelve_months_splits_evenly_with_month_end_due_dates()
    {
        var expense = Guid.NewGuid();
        var prepaid = Guid.NewGuid();
        OperationTemplate? shadow = null;
        _engine.Setup(e => e.GenerateAsync(It.IsAny<OperationTemplate>(), Vahed, It.IsAny<DateOnly>(), It.IsAny<IReadOnlyDictionary<string, string?>>(), It.IsAny<CancellationToken>()))
            .Callback<OperationTemplate, string, DateOnly, IReadOnlyDictionary<string, string?>, CancellationToken>((t, _, _, _, _) => shadow = t)
            .ReturnsAsync(EngineResult.Ok(new VoucherDraft(Vahed, Jalali(1405, 7, 20), "اجاره", "T",
            [
                new VoucherDraftLine(expense, "", [new VoucherDraftDetail(1, Guid.NewGuid(), Guid.NewGuid(), "مرکز")], 120_000_000, 0, "اجاره"),
                new VoucherDraftLine(prepaid, "", [], 0, 120_000_000, "اجاره"),
            ])));

        var (plan, error) = await Planner().PlanAsync(Template(kind: AccrualScheduleKind.Prepaid), Vahed, Jalali(1405, 7, 20), Values("12"), Draft(120_000_000), default);

        Assert.Null(error);
        // موتور فقط ردیف‌های «سهم ماهانه» را بدون شرط دید.
        Assert.Equal(2, shadow!.Lines.Count);
        Assert.All(shadow.Lines, l => Assert.Null(l.ConditionParamKey));
        Assert.Equal(12, plan!.Lines.Count);
        Assert.All(plan.Lines, l => Assert.Equal(10_000_000, l.Amount));
        Assert.Equal(120_000_000, plan.Lines.Sum(l => l.Amount));
        Assert.Equal("14050730", plan.Lines[0].DueDate);
        Assert.Equal("14060631", plan.Lines[^1].DueDate); // شهریور ۳۱ روزه
        Assert.Equal(Enumerable.Range(1, 12), plan.Lines.Select(l => l.LineNo));
        Assert.Equal(expense, plan.DebitAccountId);
        Assert.Equal(prepaid, plan.CreditAccountId);
        Assert.Single(plan.Sides!.Debit);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("0")]
    [InlineData("121")]
    [InlineData("سه")]
    public async Task Invalid_month_count_is_an_actionable_error(string? months)
    {
        var (plan, error) = await Planner().PlanAsync(Template(kind: AccrualScheduleKind.Prepaid), Vahed, Jalali(1405, 7, 20), Values(months), Draft(1_000), default);

        Assert.Null(plan);
        Assert.Equal(AccrualConventions.MonthsParameterKey, error!.ParameterKey);
    }

    // ─────────── قواعد ذخیرهٔ الگو ───────────

    private static List<string> Validate(OperationTemplate t)
    {
        var e = new List<string>();
        AccrualTemplateRules.Validate(t, t.Parameters.ToDictionary(p => p.Key, StringComparer.OrdinalIgnoreCase), e);
        return e;
    }

    [Fact]
    public void Prepaid_template_with_two_schedule_lines_and_months_is_valid()
        => Assert.Empty(Validate(Template(kind: AccrualScheduleKind.Prepaid)));

    [Fact]
    public void AutoReverse_and_schedule_together_are_rejected()
    {
        var t = Template(autoReverse: true, kind: AccrualScheduleKind.Prepaid);
        Assert.Contains(Validate(t), m => m.Contains("با هم ممکن نیست"));
    }

    [Fact]
    public void Schedule_lines_without_schedule_kind_are_rejected()
        => Assert.NotEmpty(Validate(Template(autoReverse: true)));

    [Fact]
    public void Schedule_kind_without_months_parameter_is_rejected()
    {
        var t = Template(kind: AccrualScheduleKind.Deferred);
        t.Parameters.RemoveAll(p => p.Key == AccrualConventions.MonthsParameterKey);
        Assert.Contains(Validate(t), m => m.Contains(AccrualConventions.MonthsParameterKey));
    }
}
