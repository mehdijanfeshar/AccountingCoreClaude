using Accounting.Application.Vouchers.YearEnd;

namespace Accounting.Application.Tests.Vouchers.YearEnd;

public sealed class YearEndPlannerTests
{
    private static readonly YearEndAccountRef OpeningInterface = new(Guid.NewGuid(), "900001", "رابط افتتاحیه");
    private static readonly IReadOnlySet<Guid> NoExclusions = new HashSet<Guid>();

    private static YearEndBalance Balance(string code, decimal net, params YearEndTafsili[] tafsilis)
        => new(Guid.NewGuid(), code, "حساب " + code, tafsilis, net);

    private static YearEndTafsili Tafsili(string code)
        => new(Guid.NewGuid(), "1", Guid.NewGuid(), code, "تفصیلی " + code);

    [Fact]
    public void Opening_makes_one_balanced_voucher_per_group_with_the_interface_line()
    {
        var balances = new[]
        {
            Balance("110001", 1_000),
            Balance("110002", 500, Tafsili("T1")),
            Balance("210001", -700),
            Balance("310001", 0),     // zero balance: no line
        };

        var plans = YearEndPlanner.PlanOpening(balances, NoExclusions, OpeningInterface, "1404");

        Assert.Equal(["1", "2"], plans.Select(p => p.Group));
        Assert.All(plans, p => Assert.Equal(p.TotalDebtor, p.TotalCreditor));

        var group1 = plans[0];
        Assert.Equal(3, group1.Lines.Count);
        var rabet1 = Assert.Single(group1.Lines, l => l.IsBalancing);
        Assert.Equal(OpeningInterface.Id, rabet1.AccountId);
        Assert.Equal(1_500, rabet1.Creditor);

        var rabet2 = Assert.Single(plans[1].Lines, l => l.IsBalancing);
        Assert.Equal(700, rabet2.Debtor);
    }

    [Fact]
    public void Opening_keeps_each_account_and_tafsili_combination_and_its_side()
    {
        var t = Tafsili("T9");
        var plans = YearEndPlanner.PlanOpening([Balance("110001", -250, t)], NoExclusions, OpeningInterface, "1404");

        var line = Assert.Single(plans[0].Lines, l => !l.IsBalancing);
        Assert.Equal(250, line.Creditor);
        Assert.Equal(0, line.Debtor);
        Assert.Equal(t, Assert.Single(line.Tafsilis));
    }

    [Fact]
    public void Opening_skips_excluded_accounts()
    {
        var excluded = Balance("110001", 1_000);
        var plans = YearEndPlanner.PlanOpening([excluded], new HashSet<Guid> { excluded.AccountId }, OpeningInterface, "1404");

        Assert.Empty(plans);
    }

    [Fact]
    public void Closing_reverses_each_balance_and_balances_each_rabet()
    {
        var revenue = Balance("710001", -3_000);   // credit balance
        var expense = Balance("610001", 1_200);    // debit balance
        var other = Balance("810001", 400);
        var rabetA = new YearEndAccountRef(Guid.NewGuid(), "330001", "سود و زیان");
        var rabetB = new YearEndAccountRef(Guid.NewGuid(), "330002", "رابط دوم");
        var map = new Dictionary<Guid, YearEndAccountRef>
        {
            [revenue.AccountId] = rabetA,
            [expense.AccountId] = rabetA,
            [other.AccountId] = rabetB,
        };

        var (plan, unmapped) = YearEndPlanner.PlanClosing([revenue, expense, other], NoExclusions, map, "1404");

        Assert.Empty(unmapped);
        Assert.NotNull(plan);
        Assert.Equal(plan.TotalDebtor, plan.TotalCreditor);
        Assert.Equal(3_000, Assert.Single(plan.Lines, l => l.AccountId == revenue.AccountId).Debtor);
        Assert.Equal(1_200, Assert.Single(plan.Lines, l => l.AccountId == expense.AccountId).Creditor);
        // rabetA nets -1,800 (credit) ⇒ its line is a credit of 1,800; rabetB nets +400 ⇒ debit 400.
        Assert.Equal(1_800, Assert.Single(plan.Lines, l => l.AccountId == rabetA.Id).Creditor);
        Assert.Equal(400, Assert.Single(plan.Lines, l => l.AccountId == rabetB.Id).Debtor);
    }

    [Fact]
    public void Closing_reports_accounts_without_a_rabet()
    {
        var orphan = Balance("610009", 50);
        var (_, unmapped) = YearEndPlanner.PlanClosing([orphan], NoExclusions, new Dictionary<Guid, YearEndAccountRef>(), "1404");

        Assert.Equal(orphan, Assert.Single(unmapped));
    }
}
