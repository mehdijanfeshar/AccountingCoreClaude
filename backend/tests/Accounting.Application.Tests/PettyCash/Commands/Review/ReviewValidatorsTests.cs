using System.Globalization;
using Accounting.Application.Commands.PettyCash.BulkApprovePettyCashExpenseDocs;
using Accounting.Application.Commands.PettyCash.RejectPettyCashExpenseDoc;
using Accounting.Application.Commands.PettyCash.ReturnPettyCashExpenseDoc;
using FluentValidation.TestHelper;

namespace Accounting.Application.Tests.PettyCash.Commands.Review;

public class ReviewValidatorsTests
{
    private static string JalaliPlusDays(int days)
    {
        var cal = new PersianCalendar();
        var d = DateTime.Now.AddDays(days);
        return $"{cal.GetYear(d):0000}{cal.GetMonth(d):00}{cal.GetDayOfMonth(d):00}";
    }

    private readonly ReturnPettyCashExpenseDocCommandValidator _ret = new();
    private readonly RejectPettyCashExpenseDocCommandValidator _rej = new();
    private readonly BulkApprovePettyCashExpenseDocsCommandValidator _bulk = new();

    private static ReturnPettyCashExpenseDocCommand Ret(int[]? reasons = null, string? deadline = null, string? note = "توضیح")
        => new(Guid.NewGuid(), reasons ?? new[] { 1 }, deadline ?? JalaliPlusDays(10), note);

    [Fact]
    public void Return_Valid_Passes() => _ret.TestValidate(Ret()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Return_NoReasons_Fails()
        => _ret.TestValidate(Ret(reasons: Array.Empty<int>())).ShouldHaveValidationErrorFor(x => x.ReasonCodes);

    [Fact]
    public void Return_UnknownReasonCode_Fails()
        => _ret.TestValidate(Ret(reasons: new[] { 99 })).ShouldHaveValidationErrorFor("ReasonCodes[0]");

    [Fact]
    public void Return_DeadlineBadFormat_Fails()
        => _ret.TestValidate(Ret(deadline: "1404/01/01")).ShouldHaveValidationErrorFor(x => x.Deadline);

    [Fact]
    public void Return_DeadlineInPast_Fails()
        => _ret.TestValidate(Ret(deadline: JalaliPlusDays(-5))).ShouldHaveValidationErrorFor(x => x.Deadline);

    [Fact]
    public void Return_EmptyNote_Fails()
        => _ret.TestValidate(Ret(note: "")).ShouldHaveValidationErrorFor(x => x.Note);

    [Fact]
    public void Return_NoteTooLong_Fails()
        => _ret.TestValidate(Ret(note: new string('x', 1001))).ShouldHaveValidationErrorFor(x => x.Note);

    [Fact]
    public void Reject_WithNote_Passes()
        => _rej.TestValidate(new RejectPettyCashExpenseDocCommand(Guid.NewGuid(), "دلیل")).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Reject_WithoutNote_Fails()
        => _rej.TestValidate(new RejectPettyCashExpenseDocCommand(Guid.NewGuid(), null)).ShouldHaveValidationErrorFor(x => x.Note);

    [Fact]
    public void Bulk_EmptyIds_Fails()
        => _bulk.TestValidate(new BulkApprovePettyCashExpenseDocsCommand(Array.Empty<Guid>())).ShouldHaveValidationErrorFor(x => x.Ids);

    [Fact]
    public void Bulk_EmptyGuidInList_Fails()
        => _bulk.TestValidate(new BulkApprovePettyCashExpenseDocsCommand(new[] { Guid.Empty })).ShouldHaveValidationErrorFor("Ids[0]");

    [Fact]
    public void Bulk_ValidIds_Passes()
        => _bulk.TestValidate(new BulkApprovePettyCashExpenseDocsCommand(new[] { Guid.NewGuid() })).ShouldNotHaveAnyValidationErrors();
}
