using System.Runtime.CompilerServices;
using Accounting.Application.Common;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Queries.PettyCash;
using Accounting.Application.Queries.PettyCash.GetPettyCashAttachmentFile;
using Accounting.Application.Queries.PettyCash.GetPettyCashAttachments;
using Accounting.Application.Queries.PettyCash.GetPettyCashDocEvents;
using Accounting.Application.Queries.PettyCash.GetPettyCashExpenseDocById;
using Accounting.Application.Queries.PettyCash.GetPettyCashExpenseDocs;
using Accounting.Application.Queries.PettyCash.GetPettyCashFundById;
using Accounting.Application.Queries.PettyCash.GetPettyCashFundLedger;
using Accounting.Application.Queries.PettyCash.GetPettyCashFundReviewers;
using Accounting.Application.Queries.PettyCash.GetPettyCashFundSettlements;
using Accounting.Application.Queries.PettyCash.GetPettyCashFundTafsilis;
using Accounting.Application.Queries.PettyCash.GetPettyCashFunds;
using Accounting.Application.Queries.PettyCash.GetPettyCashRefunds;
using Accounting.Application.Queries.PettyCash.GetPettyCashReplenishmentById;
using Accounting.Application.Queries.PettyCash.GetPettyCashReplenishmentPreview;
using Accounting.Application.Queries.PettyCash.GetPettyCashReplenishments;
using Accounting.Domain.ValueObjects;
using FluentValidation.TestHelper;
using Moq;

namespace Accounting.Application.Tests.PettyCash.Queries;

/// <summary>هندلرها و ولیدیتورهای کوئری‌های ساده تنخواه (Read repository ها Mock هستند).</summary>
public class PettyCashQueriesTests
{
    private static readonly Guid Id = Guid.NewGuid();
    private readonly CancellationToken _ct = default;

    // رکوردهای بزرگ فقط به‌عنوان شیء مات عبور داده می‌شوند.
    private static T Opaque<T>() where T : class => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));

    internal static PettyCashFundDto Fund(
        decimal ceiling = 1000m, decimal cash = 600m, decimal approved = 200m, int approvedCount = 2,
        decimal inFlight = 200m, int inFlightCount = 1, int? alert = null, bool deleted = false)
        => new(Id, "F1", "تنخواه", "1234567890", "علی", ceiling, 500m, 800m, alert, null, null,
            PettyCashSettlementPeriod.Monthly, true, deleted, cash, approved, approvedCount, inFlight, inFlightCount,
            PettyCashRefundRecorder.Treasurer);

    // ---------- GetPettyCashFunds / ById ----------

    [Fact]
    public async Task Funds_DelegatesWithVahedCode()
    {
        var repo = new Mock<IPettyCashFundReadRepository>();
        var list = new List<PettyCashFundDto> { Fund() };
        repo.Setup(r => r.GetAllAsync("0101", It.IsAny<CancellationToken>())).ReturnsAsync(list);

        var r = await new GetPettyCashFundsQueryHandler(repo.Object).Handle(new GetPettyCashFundsQuery { VahedCode = "0101" }, _ct);

        Assert.Same(list, r);
    }

    [Fact]
    public async Task FundById_DelegatesAndReturnsNullWhenMissing()
    {
        var repo = new Mock<IPettyCashFundReadRepository>();
        repo.Setup(r => r.GetByIdAsync(Id, "0101", It.IsAny<CancellationToken>())).ReturnsAsync((PettyCashFundDto?)null);
        var h = new GetPettyCashFundByIdQueryHandler(repo.Object);

        Assert.Null(await h.Handle(new GetPettyCashFundByIdQuery(Id) { VahedCode = "0101" }, _ct));

        var f = Fund();
        repo.Setup(r => r.GetByIdAsync(Id, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(f);
        Assert.Same(f, await h.Handle(new GetPettyCashFundByIdQuery(Id) { VahedCode = "0101" }, _ct));
    }

    [Fact]
    public void FundByIdValidator_EmptyIdFails()
    {
        var v = new GetPettyCashFundByIdQueryValidator();
        v.TestValidate(new GetPettyCashFundByIdQuery(Guid.Empty)).ShouldHaveValidationErrorFor(x => x.Id);
        v.TestValidate(new GetPettyCashFundByIdQuery(Id)).ShouldNotHaveAnyValidationErrors();
    }

    // ---------- ExpenseDoc by id / list ----------

    [Fact]
    public async Task ExpenseDocById_Delegates()
    {
        var repo = new Mock<IPettyCashExpenseDocReadRepository>();
        var dto = Opaque<PettyCashExpenseDocDto>();
        repo.Setup(r => r.GetByIdAsync(Id, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(dto);
        Assert.Same(dto, await new GetPettyCashExpenseDocByIdQueryHandler(repo.Object).Handle(new GetPettyCashExpenseDocByIdQuery(Id) { VahedCode = "0101" }, _ct));
    }

    [Fact]
    public void ExpenseDocByIdValidator_EmptyIdFails()
        => new GetPettyCashExpenseDocByIdQueryValidator().TestValidate(new GetPettyCashExpenseDocByIdQuery(Guid.Empty))
            .ShouldHaveValidationErrorFor(x => x.Id);

    [Fact]
    public async Task ExpenseDocs_BuildsFilterFromQuery()
    {
        var repo = new Mock<IPettyCashExpenseDocReadRepository>();
        PettyCashExpenseDocFilter? seen = null;
        var result = new PettyCashExpenseDocListResult(
            new PagedResult<PettyCashExpenseDocListItemDto>(), new List<PettyCashDocStateCountDto>());
        repo.Setup(r => r.GetPagedAsync(2, 20, It.IsAny<PettyCashExpenseDocFilter>(), "0101", It.IsAny<CancellationToken>()))
            .Callback<int, int, PettyCashExpenseDocFilter, string, CancellationToken>((_, _, f, _, _) => seen = f)
            .ReturnsAsync(result);
        var states = new[] { PettyCashDocState.New };

        var r = await new GetPettyCashExpenseDocsQueryHandler(repo.Object).Handle(
            new GetPettyCashExpenseDocsQuery(2, 20, Id, PettyCashDocState.Draft, states, "abc") { VahedCode = "0101" }, _ct);

        Assert.Same(result, r);
        Assert.Equal(new PettyCashExpenseDocFilter(Id, PettyCashDocState.Draft, states, "abc"), seen);
    }

    private static GetPettyCashExpenseDocsQuery DocsQ(int page = 1, int size = 20) => new(page, size) { VahedCode = "0101" };

    [Fact]
    public void ExpenseDocsValidator_PagingBounds()
    {
        var v = new GetPettyCashExpenseDocsQueryValidator();
        v.TestValidate(DocsQ()).ShouldNotHaveAnyValidationErrors();
        v.TestValidate(DocsQ(page: 0)).ShouldHaveValidationErrorFor(x => x.PageNumber);
        v.TestValidate(DocsQ(size: 0)).ShouldHaveValidationErrorFor(x => x.PageSize);
        v.TestValidate(DocsQ(size: 201)).ShouldHaveValidationErrorFor(x => x.PageSize);
        v.TestValidate(DocsQ(size: 200)).ShouldNotHaveAnyValidationErrors();
        v.TestValidate(DocsQ(page: GetPettyCashExpenseDocsQueryValidator.MaxPageNumber + 1)).ShouldHaveValidationErrorFor(x => x.PageNumber);
    }

    [Fact]
    public void ExpenseDocsValidator_StateAndSearchRules()
    {
        var v = new GetPettyCashExpenseDocsQueryValidator();
        v.TestValidate(DocsQ() with { State = (PettyCashDocState)99 }).ShouldHaveValidationErrorFor(x => x.State);
        v.TestValidate(DocsQ() with { States = new[] { PettyCashDocState.New, (PettyCashDocState)99 } }).ShouldHaveValidationErrorFor(x => x.States);
        v.TestValidate(DocsQ() with { States = new[] { PettyCashDocState.New, PettyCashDocState.Returned } }).ShouldNotHaveAnyValidationErrors();
        v.TestValidate(DocsQ() with { Search = new string('a', 201) }).ShouldHaveValidationErrorFor(x => x.Search);
    }

    // ---------- Attachments / events / file : 404 on missing parent ----------

    private readonly Mock<IPettyCashExpenseDocReadRepository> _docRead = new();
    private readonly Mock<IPettyCashAttachmentReadRepository> _attRead = new();
    private readonly Mock<IPettyCashDocEventReadRepository> _evRead = new();

    private void ParentExists() => _docRead.Setup(r => r.GetByIdAsync(Id, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(Opaque<PettyCashExpenseDocDto>());

    [Fact]
    public async Task Attachments_ParentMissing_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new GetPettyCashAttachmentsQueryHandler(_docRead.Object, _attRead.Object).Handle(new GetPettyCashAttachmentsQuery(Id) { VahedCode = "0101" }, _ct));
        _attRead.Verify(a => a.GetByExpenseDocIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Attachments_ParentExists_ReturnsList()
    {
        ParentExists();
        var list = new List<PettyCashAttachmentDto>();
        _attRead.Setup(a => a.GetByExpenseDocIdAsync(Id, It.IsAny<CancellationToken>())).ReturnsAsync(list);
        Assert.Same(list, await new GetPettyCashAttachmentsQueryHandler(_docRead.Object, _attRead.Object)
            .Handle(new GetPettyCashAttachmentsQuery(Id) { VahedCode = "0101" }, _ct));
    }

    [Fact]
    public async Task AttachmentFile_ParentMissing_ThrowsNotFound()
        => await Assert.ThrowsAsync<NotFoundException>(() =>
            new GetPettyCashAttachmentFileQueryHandler(_docRead.Object, _attRead.Object).Handle(new GetPettyCashAttachmentFileQuery(Id, Guid.NewGuid()) { VahedCode = "0101" }, _ct));

    [Fact]
    public async Task AttachmentFile_ParentExists_ReturnsFileOrNull()
    {
        ParentExists();
        var attId = Guid.NewGuid();
        var h = new GetPettyCashAttachmentFileQueryHandler(_docRead.Object, _attRead.Object);
        Assert.Null(await h.Handle(new GetPettyCashAttachmentFileQuery(Id, attId) { VahedCode = "0101" }, _ct));

        var file = Opaque<PettyCashAttachmentFileDto>();
        _attRead.Setup(a => a.GetFileAsync(Id, attId, It.IsAny<CancellationToken>())).ReturnsAsync(file);
        Assert.Same(file, await h.Handle(new GetPettyCashAttachmentFileQuery(Id, attId) { VahedCode = "0101" }, _ct));
    }

    [Fact]
    public void AttachmentValidators_RequireIds()
    {
        new GetPettyCashAttachmentsQueryValidator().TestValidate(new GetPettyCashAttachmentsQuery(Guid.Empty)).ShouldHaveValidationErrorFor(x => x.ExpenseDocId);
        var v = new GetPettyCashAttachmentFileQueryValidator();
        v.TestValidate(new GetPettyCashAttachmentFileQuery(Guid.Empty, Id)).ShouldHaveValidationErrorFor(x => x.ExpenseDocId);
        v.TestValidate(new GetPettyCashAttachmentFileQuery(Id, Guid.Empty)).ShouldHaveValidationErrorFor(x => x.AttachmentId);
    }

    [Fact]
    public async Task DocEvents_ParentMissing_ThrowsNotFound()
        => await Assert.ThrowsAsync<NotFoundException>(() =>
            new GetPettyCashDocEventsQueryHandler(_docRead.Object, _evRead.Object).Handle(new GetPettyCashDocEventsQuery(Id) { VahedCode = "0101" }, _ct));

    [Fact]
    public async Task DocEvents_ParentExists_ReturnsEvents()
    {
        ParentExists();
        var list = new List<PettyCashDocEventDto>();
        _evRead.Setup(e => e.GetByExpenseDocIdAsync(Id, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(list);
        Assert.Same(list, await new GetPettyCashDocEventsQueryHandler(_docRead.Object, _evRead.Object).Handle(new GetPettyCashDocEventsQuery(Id) { VahedCode = "0101" }, _ct));
    }

    [Fact]
    public void DocEventsValidator_RequiresId()
        => new GetPettyCashDocEventsQueryValidator().TestValidate(new GetPettyCashDocEventsQuery(Guid.Empty)).ShouldHaveValidationErrorFor(x => x.ExpenseDocId);

    // ---------- Reviewers ----------

    [Fact]
    public async Task Reviewers_FundMissingOrDeleted_ThrowsNotFound()
    {
        var funds = new Mock<IPettyCashFundReadRepository>();
        var rev = new Mock<IPettyCashFundReviewerReadRepository>();
        var h = new GetPettyCashFundReviewersQueryHandler(funds.Object, rev.Object);
        var q = new GetPettyCashFundReviewersQuery(Id) { VahedCode = "0101" };

        await Assert.ThrowsAsync<NotFoundException>(() => h.Handle(q, _ct));

        funds.Setup(f => f.GetByIdAsync(Id, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(Fund(deleted: true));
        await Assert.ThrowsAsync<NotFoundException>(() => h.Handle(q, _ct));
        rev.Verify(r => r.GetByFundIdAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Reviewers_FundExists_ReturnsList()
    {
        var funds = new Mock<IPettyCashFundReadRepository>();
        var rev = new Mock<IPettyCashFundReviewerReadRepository>();
        funds.Setup(f => f.GetByIdAsync(Id, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(Fund());
        var list = new List<PettyCashFundReviewerDto>();
        rev.Setup(r => r.GetByFundIdAsync(Id, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(list);
        Assert.Same(list, await new GetPettyCashFundReviewersQueryHandler(funds.Object, rev.Object).Handle(new GetPettyCashFundReviewersQuery(Id) { VahedCode = "0101" }, _ct));
    }

    [Fact]
    public void ReviewersValidator_RequiresFundId()
        => new GetPettyCashFundReviewersQueryValidator().TestValidate(new GetPettyCashFundReviewersQuery(Guid.Empty)).ShouldHaveValidationErrorFor(x => x.FundId);

    // ---------- Ledger ----------

    [Fact]
    public async Task Ledger_PassesAllFilters()
    {
        var repo = new Mock<IPettyCashLedgerReadRepository>();
        var dto = Opaque<PettyCashFundLedgerDto>();
        repo.Setup(r => r.GetAsync(Id, "14040101", "14040131", "expense", "0101", It.IsAny<CancellationToken>())).ReturnsAsync(dto);
        Assert.Same(dto, await new GetPettyCashFundLedgerQueryHandler(repo.Object)
            .Handle(new GetPettyCashFundLedgerQuery(Id, "14040101", "14040131", "expense") { VahedCode = "0101" }, _ct));
    }

    [Fact]
    public void LedgerValidator_Rules()
    {
        var v = new GetPettyCashFundLedgerQueryValidator();
        v.TestValidate(new GetPettyCashFundLedgerQuery(Id, null, null, null)).ShouldNotHaveAnyValidationErrors();
        v.TestValidate(new GetPettyCashFundLedgerQuery(Guid.Empty, null, null, null)).ShouldHaveValidationErrorFor(x => x.FundId);
        v.TestValidate(new GetPettyCashFundLedgerQuery(Id, "1404", null, null)).ShouldHaveValidationErrorFor(x => x.From);
        v.TestValidate(new GetPettyCashFundLedgerQuery(Id, null, "abcdefgh", null)).ShouldHaveValidationErrorFor(x => x.To);
        v.TestValidate(new GetPettyCashFundLedgerQuery(Id, "14040201", "14040101", null)).ShouldHaveAnyValidationError_();
        v.TestValidate(new GetPettyCashFundLedgerQuery(Id, "14040101", "14040101", null)).ShouldNotHaveAnyValidationErrors();
        v.TestValidate(new GetPettyCashFundLedgerQuery(Id, null, null, "bogus")).ShouldHaveValidationErrorFor(x => x.Type);
        foreach (var t in new[] { "replenishment", "expense", "refund" })
            v.TestValidate(new GetPettyCashFundLedgerQuery(Id, null, null, t)).ShouldNotHaveAnyValidationErrors();
    }

    // ---------- Settlements history / fund tafsilis ----------

    [Fact]
    public async Task SettlementHistoryAndTafsilis_Delegate()
    {
        var repo = new Mock<IPettyCashSettlementReadRepository>();
        var hist = new List<PettyCashSettlementHistoryItemDto>();
        var taf = new List<PettyCashSettlementTafsiliDto>();
        repo.Setup(r => r.GetFinalHistoryAsync(Id, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(hist);
        repo.Setup(r => r.GetFundTafsilisAsync(Id, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(taf);

        Assert.Same(hist, await new GetPettyCashFundSettlementsQueryHandler(repo.Object).Handle(new GetPettyCashFundSettlementsQuery(Id) { VahedCode = "0101" }, _ct));
        Assert.Same(taf, await new GetPettyCashFundTafsilisQueryHandler(repo.Object).Handle(new GetPettyCashFundTafsilisQuery(Id) { VahedCode = "0101" }, _ct));
    }

    // ---------- Refunds ----------

    [Fact]
    public async Task Refunds_DelegatesAndValidates()
    {
        var repo = new Mock<IPettyCashRefundReadRepository>();
        var list = new List<PettyCashRefundDto>();
        repo.Setup(r => r.GetByFundAsync(Id, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(list);
        Assert.Same(list, await new GetPettyCashRefundsQueryHandler(repo.Object).Handle(new GetPettyCashRefundsQuery(Id) { VahedCode = "0101" }, _ct));
        new GetPettyCashRefundsQueryValidator().TestValidate(new GetPettyCashRefundsQuery(Guid.Empty)).ShouldHaveValidationErrorFor(x => x.FundId);
    }

    // ---------- Replenishments ----------

    [Fact]
    public async Task ReplenishmentById_Delegates()
    {
        var repo = new Mock<IPettyCashReplenishmentReadRepository>();
        var dto = Opaque<PettyCashReplenishmentDto>();
        repo.Setup(r => r.GetByIdAsync(Id, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(dto);
        Assert.Same(dto, await new GetPettyCashReplenishmentByIdQueryHandler(repo.Object).Handle(new GetPettyCashReplenishmentByIdQuery(Id) { VahedCode = "0101" }, _ct));
        new GetPettyCashReplenishmentByIdQueryValidator().TestValidate(new GetPettyCashReplenishmentByIdQuery(Guid.Empty)).ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public async Task Replenishments_PassesPagingAndFilters()
    {
        var repo = new Mock<IPettyCashReplenishmentReadRepository>();
        var page = new PagedResult<PettyCashReplenishmentListItemDto>();
        repo.Setup(r => r.GetPagedAsync(3, 10, Id, PettyCashReplenishmentState.Paid, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(page);
        Assert.Same(page, await new GetPettyCashReplenishmentsQueryHandler(repo.Object)
            .Handle(new GetPettyCashReplenishmentsQuery(3, 10, Id, PettyCashReplenishmentState.Paid) { VahedCode = "0101" }, _ct));
    }

    [Fact]
    public void ReplenishmentsValidator_Rules()
    {
        var v = new GetPettyCashReplenishmentsQueryValidator();
        v.TestValidate(new GetPettyCashReplenishmentsQuery(1, 20)).ShouldNotHaveAnyValidationErrors();
        v.TestValidate(new GetPettyCashReplenishmentsQuery(0, 20)).ShouldHaveValidationErrorFor(x => x.PageNumber);
        v.TestValidate(new GetPettyCashReplenishmentsQuery(1, 0)).ShouldHaveValidationErrorFor(x => x.PageSize);
        v.TestValidate(new GetPettyCashReplenishmentsQuery(1, 201)).ShouldHaveValidationErrorFor(x => x.PageSize);
        v.TestValidate(new GetPettyCashReplenishmentsQuery(1, 20, null, (PettyCashReplenishmentState)99)).ShouldHaveValidationErrorFor(x => x.State);
    }

    [Fact]
    public async Task ReplenishmentPreview_FundMissing_ReturnsNull()
    {
        var funds = new Mock<IPettyCashFundReadRepository>();
        var repl = new Mock<IPettyCashReplenishmentReadRepository>();
        Assert.Null(await new GetPettyCashReplenishmentPreviewQueryHandler(funds.Object, repl.Object)
            .Handle(new GetPettyCashReplenishmentPreviewQuery(Id) { VahedCode = "0101" }, _ct));
        repl.Verify(r => r.GetUnlinkedApprovedGroupedAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReplenishmentPreview_ComputesBalanceAfter()
    {
        var funds = new Mock<IPettyCashFundReadRepository>();
        var repl = new Mock<IPettyCashReplenishmentReadRepository>();
        funds.Setup(f => f.GetByIdAsync(Id, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(Fund(cash: 600m, approved: 300m, approvedCount: 3));
        var docIds = new List<Guid> { Guid.NewGuid() };
        repl.Setup(r => r.GetUnlinkedApprovedGroupedAsync(Id, "0101", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PettyCashReplenishmentPreviewLinesResult(new List<PettyCashReplenishmentLineDto>(), 300m, docIds));

        var r = await new GetPettyCashReplenishmentPreviewQueryHandler(funds.Object, repl.Object)
            .Handle(new GetPettyCashReplenishmentPreviewQuery(Id) { VahedCode = "0101" }, _ct);

        Assert.Equal(300m, r!.TotalAmount);
        Assert.Equal(900m, r.BalanceAfter);
        Assert.Equal(3, r.ApprovedCount);
        Assert.Same(docIds, r.DocIds);
    }

    [Fact]
    public void PreviewValidators_RequireFundId()
    {
        new GetPettyCashReplenishmentPreviewQueryValidator().TestValidate(new GetPettyCashReplenishmentPreviewQuery(Guid.Empty)).ShouldHaveValidationErrorFor(x => x.FundId);
    }
}

internal static class ValidationExtensions
{
    public static void ShouldHaveAnyValidationError_<T>(this TestValidationResult<T> r) where T : class
        => Assert.False(r.IsValid);
}
