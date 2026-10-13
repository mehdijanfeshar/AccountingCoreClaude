using Accounting.Application.FinancialStatements.Approvals;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Tests.FinancialStatements.Approvals;

/// <summary>انتخاب زنجیرهٔ مراحل تأیید و قاعدهٔ «چه کسی نمی‌تواند تأیید کند».</summary>
public sealed class FsApprovalChainTests
{
    private static TB_FS_APPROVAL_STEP Step(string? owner, int no, FsFramework fw = FsFramework.Pension, bool active = true, string? approvers = null, string title = "مرحله")
        => new() { ID = Guid.NewGuid(), VAHEDCODE = owner, STEP_NO = no, FRAMEWORK = fw, IS_ACTIVE = active, APPROVER_USERIDS = approvers, TITLE_FA = title + no };

    // ───────── Resolve ─────────

    [Fact]
    public void Resolve_NoSteps_ReturnsEmpty()
    {
        Assert.Empty(FsApprovalChain.Resolve([], FsTestData.ProvinceScope(), FsFramework.Pension));
    }

    [Fact]
    public void Resolve_OrdersByStepNo()
    {
        var chain = FsApprovalChain.Resolve([Step(null, 2), Step(null, 1), Step(null, 3)], FsTestData.ProvinceScope(), FsFramework.Pension);

        Assert.Equal(new[] { 1, 2, 3 }, chain.Select(s => s.STEP_NO).ToArray());
    }

    [Fact]
    public void Resolve_NearestOwnerWins_OverParentAndShared()
    {
        var own = Step("10", 1);
        var parent = Step("1", 1);
        var shared = Step(null, 1);

        var chain = FsApprovalChain.Resolve([shared, parent, own], FsTestData.ProvinceScope(), FsFramework.Pension);

        Assert.Same(own, Assert.Single(chain));
    }

    [Fact]
    public void Resolve_ParentOverridesShared_WhenUnitHasNone()
    {
        var parent = Step("1", 1);

        var chain = FsApprovalChain.Resolve([Step(null, 1), parent], FsTestData.ProvinceScope(), FsFramework.Pension);

        Assert.Same(parent, Assert.Single(chain));
    }

    [Fact]
    public void Resolve_FallsBackToShared_WhenNoOwnOrAncestor()
    {
        var shared = Step(null, 1);

        var chain = FsApprovalChain.Resolve([shared, Step("20", 1)], FsTestData.ProvinceScope(), FsFramework.Pension);

        Assert.Same(shared, Assert.Single(chain));
    }

    [Fact]
    public void Resolve_IgnoresInactiveSteps_AndOtherFrameworks_AndUnrelatedUnits()
    {
        var steps = new[]
        {
            Step("10", 1, active: false),
            Step("10", 1, fw: FsFramework.Commercial),
            Step("20", 1),
            Step("101", 1),   // زیرمجموعه: مرحلهٔ واحد پایین‌تر روی واحد بالاتر اثر ندارد
        };

        Assert.Empty(FsApprovalChain.Resolve(steps, FsTestData.ProvinceScope(), FsFramework.Pension));
    }

    [Fact]
    public void Resolve_InactiveOwnSteps_FallThroughToNextOwner()
    {
        var parent = Step("1", 1);

        var chain = FsApprovalChain.Resolve([Step("10", 1, active: false), parent], FsTestData.ProvinceScope(), FsFramework.Pension);

        Assert.Same(parent, Assert.Single(chain));
    }

    // ───────── Approvers ─────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Approvers_Empty(string? text) => Assert.Empty(FsApprovalChain.Approvers(text));

    [Fact]
    public void Approvers_SplitsOnAllSeparators_Trims_DedupesIgnoringCase_AndNormalizesDigits()
    {
        var result = FsApprovalChain.Approvers("۱۲۳, abc ،ABC;def\n 123");

        Assert.Equal(new[] { "123", "abc", "def" }, result.ToArray());
    }

    // ───────── CannotApprove ─────────

    [Fact]
    public void CannotApprove_Preparer_IsRejected()
    {
        var reason = FsApprovalChain.CannotApprove(null, "u1", "u1", "u2", []);

        Assert.NotNull(reason);
        Assert.Contains("تفکیک وظایف", reason);
    }

    [Fact]
    public void CannotApprove_Submitter_IsRejected()
    {
        Assert.NotNull(FsApprovalChain.CannotApprove(null, "u2", "u1", "u2", []));
    }

    [Fact]
    public void CannotApprove_NullSubmitter_DoesNotBlockOthers()
    {
        Assert.Null(FsApprovalChain.CannotApprove(null, "u3", "u1", null, []));
    }

    [Fact]
    public void CannotApprove_EarlierStepApprover_IsRejected_CaseInsensitive()
    {
        var reason = FsApprovalChain.CannotApprove(null, "U3", "u1", "u2", ["u3"]);

        Assert.NotNull(reason);
        Assert.Contains("مرحلهٔ قبلی", reason);
    }

    [Fact]
    public void CannotApprove_NotInStepApprovers_IsRejected_WithNamesInMessage()
    {
        var step = Step("10", 1, approvers: "alice, bob", title: "مالی");

        var reason = FsApprovalChain.CannotApprove(step, "carol", "u1", "u2", []);

        Assert.NotNull(reason);
        Assert.Contains("alice", reason);
        Assert.Contains("bob", reason);
        Assert.Contains("مالی", reason);
    }

    [Fact]
    public void CannotApprove_InStepApprovers_IsAllowed_CaseInsensitive()
    {
        var step = Step("10", 1, approvers: "alice, bob");

        Assert.Null(FsApprovalChain.CannotApprove(step, "ALICE", "u1", "u2", []));
    }

    [Fact]
    public void CannotApprove_StepWithoutApproverList_AllowsAnyoneElse()
    {
        var step = Step("10", 1, approvers: null);

        Assert.Null(FsApprovalChain.CannotApprove(step, "anyone", "u1", "u2", []));
    }

    [Fact]
    public void CannotApprove_SegregationBeatsApproverList()
    {
        var step = Step("10", 1, approvers: "u1");

        // u1 در فهرست هست ولی تهیه‌کننده است.
        Assert.NotNull(FsApprovalChain.CannotApprove(step, "u1", "u1", null, []));
    }
}
