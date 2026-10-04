using System.Text.Json.Serialization;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.FinancialStatements.Approvals;
using Accounting.Application.FinancialStatements.Commands.TransitionFsRun;
using Accounting.Application.FinancialStatements.Queries;
using Accounting.Application.FinancialStatements.Ratios;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.FinancialStatements.Dashboard;

// ح-۹ (docs/fs-module.md §۱۳) — داشبورد صورت‌های مالی (سند منبع §۱۲-۳): شاخص‌های اصلی از آخرین اجرای منتشرشده
// (وگرنه آخرین اجرای جاری)، و «کارهای من» بر اساس دامنهٔ کاربر. وضعیت بستن واحدها از صفحهٔ بستن دوره می‌آید.

/// <summary>یک کار: <paramref name="Kind"/> = approveRun | publishRun | check | reviewNarrative | reviseNarrative | reopenRequest.</summary>
public sealed record FsTaskDto(string Kind, string Title, string? Detail, string Link, DateTime? Date);

/// <param name="Run">اجرایی که شاخص‌ها از آن خوانده شده‌اند (<c>null</c> = اجرایی نیست).</param>
public sealed record FsDashboardDto(FsRunSummaryDto? Run, IReadOnlyList<FsRatioValueDto> Kpis, IReadOnlyList<FsTaskDto> Tasks);

/// <summary><c>GET api/fs/dashboard?framework=&amp;year=</c> — واحد هدر.</summary>
public sealed record GetFsDashboardQuery(FsFramework Framework, string Year) : IRequest<FsDashboardDto>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class GetFsDashboardQueryHandler : IRequestHandler<GetFsDashboardQuery, FsDashboardDto>
{
    private const int KpiCount = 4;

    private readonly IFsRunRepository _runs;
    private readonly IFsRatioRepository _ratios;
    private readonly IFsApprovalStepRepository _steps;
    private readonly IFsNarrativeRepository _narratives;
    private readonly IFsPeriodRepository _periods;
    private readonly IFsUnitScopeProvider _scopes;
    private readonly ICurrentUser _currentUser;

    public GetFsDashboardQueryHandler(
        IFsRunRepository runs,
        IFsRatioRepository ratios,
        IFsApprovalStepRepository steps,
        IFsNarrativeRepository narratives,
        IFsPeriodRepository periods,
        IFsUnitScopeProvider scopes,
        ICurrentUser currentUser)
    {
        _runs = runs;
        _ratios = ratios;
        _steps = steps;
        _narratives = narratives;
        _periods = periods;
        _scopes = scopes;
        _currentUser = currentUser;
    }

    public async Task<FsDashboardDto> Handle(GetFsDashboardQuery request, CancellationToken cancellationToken)
    {
        var scope = await _scopes.GetAsync(request.VahedCode, cancellationToken);
        var user = _currentUser.UserId;
        var runs = (await _runs.ListAsync(request.VahedCode, request.Year, cancellationToken))
            .Where(r => r.Framework == request.Framework && r.State != FsRunState.Superseded)
            .ToList();

        var kpiRun = runs.Where(r => r.State == FsRunState.Published).OrderByDescending(r => r.CreatedDate).FirstOrDefault()
            ?? runs.OrderByDescending(r => r.CreatedDate).FirstOrDefault();
        IReadOnlyList<FsRatioValueDto> kpis = [];

        if (kpiRun is not null && await _runs.GetDetailAsync(kpiRun.Id, request.VahedCode, cancellationToken) is { } kpiDetail)
        {
            var effective = FsRatioEngine.Effective(await _ratios.GetAllAsync(request.Framework, cancellationToken), scope, request.Framework);
            kpis = FsRatioEngine.Evaluate(effective.Take(KpiCount).ToList(), kpiDetail);
        }

        var tasks = new List<FsTaskDto>();

        // ۱) اجراهای در بازبینی که مرحلهٔ جاری‌شان را من می‌توانم تأیید کنم؛ ۲) تأییدشده‌های آمادهٔ انتشار.
        var chain = FsApprovalChain.Resolve(await _steps.GetAllAsync(request.Framework, cancellationToken), scope, request.Framework);

        foreach (var r in runs.Where(r => r.State is FsRunState.InReview or FsRunState.Approved))
        {
            var detail = await _runs.GetDetailAsync(r.Id, request.VahedCode, cancellationToken);

            if (detail is null)
            {
                continue;
            }

            if (r.State == FsRunState.InReview)
            {
                var cycle = TransitionFsRunCommandHandler.CurrentCycleApprovals(detail.Actions);
                var stepIndex = cycle.Count(a => a.StepNo is not null);
                var step = stepIndex < chain.Count ? chain[stepIndex] : null;
                var submitter = detail.Actions.OrderBy(a => a.CreatedDate).LastOrDefault(a => a.Action == FsRunAction.Submit)?.UserId;

                if (FsApprovalChain.CannotApprove(step, user, r.AddUserId, submitter, cycle.Select(a => a.UserId)) is null)
                {
                    tasks.Add(new FsTaskDto(
                        "approveRun",
                        $"تأیید اجرای شمارهٔ {r.RunNo}" + (step is null ? string.Empty : $" — مرحلهٔ «{step.TITLE_FA}»"),
                        null,
                        $"/fs/runs/{r.Id}",
                        r.CreatedDate));
                }
            }
            else if (r.AddUserId != user)
            {
                tasks.Add(new FsTaskDto("publishRun", $"انتشار اجرای شمارهٔ {r.RunNo}", "پیش‌شرط: قفل دورهٔ واحدهای دامنه (V-11)", $"/fs/runs/{r.Id}", r.CreatedDate));
            }
        }

        // ۳) کنترل‌های ناموفقی که به من ارجاع شده‌اند.
        foreach (var c in await _runs.GetOpenAssignmentsAsync(request.VahedCode, user, cancellationToken))
        {
            var due = c.DueDate is { Length: 8 } d ? $"مهلت {d[..4]}/{d[4..6]}/{d[6..]}" : null;
            tasks.Add(new FsTaskDto("check", $"رفع کنترل {c.Code} — اجرای {c.RunNo}", (c.TitleFa + (due is null ? string.Empty : $" · {due}")), $"/fs/runs/{c.RunId}", null));
        }

        // ۴) یادداشت‌های توضیحی: در بازبینی (که آخرین ویرایشگرش من نیستم) و نیازمند اصلاحِ با مسئولیت من.
        foreach (var n in await _narratives.GetSetAsync(request.VahedCode, request.Framework, request.Year, false, cancellationToken))
        {
            if (n.STATE == FsNarrativeState.InReview && (n.CHANGEUSERID ?? n.ADDUSERID) != user)
            {
                tasks.Add(new FsTaskDto("reviewNarrative", $"بازبینی یادداشت «{n.TITLE_FA}»", null, "/fs/narratives", n.UPDATEDDATE ?? n.CREATEDDATE));
            }
            else if (n.STATE == FsNarrativeState.NeedsRevision && n.RESPONSIBLE_USERID == user)
            {
                tasks.Add(new FsTaskDto("reviseNarrative", $"اصلاح یادداشت «{n.TITLE_FA}»", n.REVIEW_COMMENT, "/fs/narratives", n.UPDATEDDATE ?? n.CREATEDDATE));
            }
        }

        // ۵) درخواست‌های بازگشایی دوره در زیرمجموعه (فقط ستاد تأیید می‌کند).
        if (scope.IsHeadquarters)
        {
            foreach (var p in (await _periods.GetForYearAsync(request.Year, cancellationToken))
                         .Where(p => p.REOPEN_REQUESTED_BY is not null && scope.Accessible.Contains(p.VAHEDCODE) && p.REOPEN_REQUESTED_BY != user))
            {
                tasks.Add(new FsTaskDto(
                    "reopenRequest",
                    $"درخواست بازگشایی دورهٔ واحد {p.VAHEDCODE}",
                    p.REOPEN_REASON,
                    "/fs/period-close",
                    p.REOPEN_REQUESTED_DATE));
            }
        }

        return new FsDashboardDto(kpiRun, kpis, tasks);
    }
}
