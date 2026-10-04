using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.FinancialStatements.Consolidation;
using Accounting.Application.FinancialStatements.Engine;
using Accounting.Application.FinancialStatements.Queries;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.FinancialStatements.Commands.GenerateFsRun;

/// <summary>
/// ۱) دامنهٔ واحد (<see cref="FsUnitScope"/>)؛ ۲) نسخهٔ قالب هر صورت (قالب اختصاصی نزدیک‌ترین مالک بر
/// مشترک مقدم)؛ ۳) ماندهٔ معین‌ها به تفکیک واحد، یک کوئری در هر ستون؛ ۴) <see cref="FsStatementEngine"/>
/// روی جمع واحدها؛ ۵) Snapshot در یک SaveChanges — سهم هر معین به تفکیک زیرواحد سطح اول
/// (<c>docs/fs-module.md</c> §۸). خطای قالب/موتور ⇒ ۴۰۹ با پیام فارسی.
/// </summary>
public sealed class GenerateFsRunCommandHandler : IRequestHandler<GenerateFsRunCommand, Guid>
{
    private readonly IFsTemplateReadRepository _templateReadRepository;
    private readonly IFsBalanceReadRepository _balanceReadRepository;
    private readonly IFsRunRepository _runRepository;
    private readonly IFsUnitScopeProvider _scopes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IFsCheckRuleRepository _ruleRepository;
    private readonly IFsConsolidationRepository _consolidation;
    private readonly IUnitAccessReadRepository _unitAccess;

    public GenerateFsRunCommandHandler(
        IFsTemplateReadRepository templateReadRepository,
        IFsBalanceReadRepository balanceReadRepository,
        IFsRunRepository runRepository,
        IFsUnitScopeProvider scopes,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IFsCheckRuleRepository ruleRepository,
        IFsConsolidationRepository consolidation,
        IUnitAccessReadRepository unitAccess)
    {
        _templateReadRepository = templateReadRepository;
        _balanceReadRepository = balanceReadRepository;
        _runRepository = runRepository;
        _scopes = scopes;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _ruleRepository = ruleRepository;
        _consolidation = consolidation;
        _unitAccess = unitAccess;
    }

    public async Task<Guid> Handle(GenerateFsRunCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var year = int.Parse(request.Year, CultureInfo.InvariantCulture);
        var scope = await _scopes.GetAsync(request.VahedCode, cancellationToken);

        var versions = await _templateReadRepository.GetVersionsForRunAsync(
            request.Framework, year, request.UseDraftVersions, scope, cancellationToken);

        if (versions.Count == 0)
        {
            throw new FsTemplateConflictException(request.UseDraftVersions
                ? "این مجموعه برای این واحد هیچ قالبی (فعال یا پیش‌نویس) ندارد."
                : $"این مجموعه برای سال {request.Year} هیچ قالب فعالی ندارد. قالب‌ها را فعال کنید یا گزینهٔ «استفاده از پیش‌نویس‌ها» را بزنید.");
        }

        // بخش ۴۵-ه — اجرای مبدأ (جایگزینی) و مقادیر دستی.
        TB_FS_RUN? sourceRun = null;

        if (request.SourceRunId is { } sourceId)
        {
            sourceRun = await _runRepository.GetForUpdateAsync(sourceId, request.VahedCode, cancellationToken)
                ?? throw new NotFoundException("FsRun", sourceId);

            if (sourceRun.STATE != FsRunState.Draft)
            {
                throw new FsTemplateConflictException("فقط اجرای پیش‌نویس با اجرای تازه جایگزین می‌شود؛ اجرای ارسال‌شده یا تأییدشده را اول برگردانید.");
            }
        }

        var external = BuildExternalValues(versions, request.ManualValues ?? Array.Empty<FsManualValueInput>());

        var unitCodes = request.IncludeSubUnits ? scope.Accessible.ToList() : new List<string> { request.VahedCode };

        // ماندهٔ خام به تفکیک (معین، واحد) برای هر ستون.
        var (fromDate, toDate) = Period(year, request.ToMonth);
        var raw = new Dictionary<string, IReadOnlyList<FsAccountBalance>>(StringComparer.Ordinal)
        {
            [FsColumns.Current] = await _balanceReadRepository.GetBalancesAsync(
                request.Year, fromDate, toDate, unitCodes, request.MinDocLife, cancellationToken),
        };

        if (request.IncludePrior)
        {
            var (priorFrom, priorTo) = Period(year - 1, request.ToMonth);
            raw[FsColumns.Prior] = await _balanceReadRepository.GetBalancesAsync(
                (year - 1).ToString(CultureInfo.InvariantCulture), priorFrom, priorTo, unitCodes, request.MinDocLife, cancellationToken);
        }

        // ط-۴..ط-۷ — افزوده‌ها روی مانده‌های اسناد. «raw» دست‌نخورده می‌ماند (اثر انگشت کهنگی فقط اسناد را می‌سنجد).
        var all = raw.ToDictionary(kv => kv.Key, kv => kv.Value.ToList(), StringComparer.Ordinal);
        var extra = await ApplyAdditionsAsync(request, scope, year, fromDate, toDate, unitCodes, all, cancellationToken);

        string GroupOf(string? unit) =>
            unit is not null && (unit == FsSyntheticAccounts.EliminationGroup || extra.EntityNames.ContainsKey(unit))
                ? unit
                : request.IncludeSubUnits ? scope.GroupOf(unit ?? request.VahedCode) : request.VahedCode;

        static FsAccountBalance Sum(string code, IEnumerable<FsAccountBalance> items, string? unit = null)
        {
            var list = items.ToList();
            return new FsAccountBalance(
                code,
                list.Select(b => b.AccName).FirstOrDefault(n => n is not null),
                list.Sum(b => b.OpeningDebtor),
                list.Sum(b => b.OpeningCreditor),
                list.Sum(b => b.PeriodDebtor),
                list.Sum(b => b.PeriodCreditor),
                unit,
                list.Sum(b => b.CashFlow));
        }

        // جمع واحدها برای موتور، و سهم هر گروه (زیرواحد سطح اول، شرکت تابعه، حذفیات) برای Drill-down و کاربرگ.
        var totals = all.ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlyList<FsAccountBalance>)kv.Value
                .GroupBy(b => b.AccCode, StringComparer.Ordinal)
                .Select(g => Sum(g.Key, g))
                .ToList(),
            StringComparer.Ordinal);

        var byGroup = all.ToDictionary(
            kv => kv.Key,
            kv => kv.Value
                .GroupBy(b => b.AccCode, StringComparer.Ordinal)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(b => GroupOf(b.VahedCode), StringComparer.Ordinal)
                        .ToDictionary(u => u.Key, u => Sum(g.Key, u), StringComparer.Ordinal),
                    StringComparer.Ordinal),
            StringComparer.Ordinal);

        IReadOnlyDictionary<(string Stmt, string Row), IReadOnlyDictionary<string, FsRowValue>> values;

        try
        {
            values = FsStatementEngine.Compute(
                versions.Select(v => new FsEngineStatement(
                    v.TemplateCode,
                    v.Rows.Select(r => new FsEngineRow(r.Code, r.OrderNo, r.RowType, r.Selector, r.ValueType, r.Formula)).ToList()))
                    .ToList(),
                totals,
                external);
        }
        catch (Exception ex) when (ex is FsEngineException or FsExpressionException or KeyNotFoundException)
        {
            throw new FsTemplateConflictException(
                "محاسبهٔ صورت‌ها ممکن نشد: " + ex.Message + " — «بررسی قالب» را در صفحهٔ قالب اجرا کنید.");
        }

        var accountNames = totals.Values
            .SelectMany(b => b)
            .GroupBy(b => b.AccCode, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().AccName, StringComparer.Ordinal);

        var now = DateTime.UtcNow;
        var run = new TB_FS_RUN
        {
            ID = Guid.NewGuid(),
            RUN_NO = await _runRepository.GetNextRunNoAsync(cancellationToken),
            VAHEDCODE = request.VahedCode,
            VAHEDNAME = scope.Names.GetValueOrDefault(request.VahedCode),
            INCLUDE_SUBUNITS = request.IncludeSubUnits,
            UNIT_COUNT = unitCodes.Count,
            FRAMEWORK = request.Framework,
            YEAR = request.Year,
            TO_MONTH = request.ToMonth,
            FROM_DATE = fromDate,
            TO_DATE = toDate,
            MIN_DOCLIFE = request.MinDocLife,
            HAS_PRIOR = request.IncludePrior,
            PRIOR_RESTATED = request.IncludePrior && request.PriorRestated,
            INCLUDE_ENTITIES = request.IncludeEntities,
            USES_DRAFT = versions.Any(v => v.State == FsTemplateVersionState.Draft),
            STATE = FsRunState.Draft,
            DESCRIPTION = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CREATEDDATE = now,
            ADDUSERID = _currentUser.UserId,
        };

        // بخش ۴۵-ج — شمارهٔ یادداشت‌ها و زیر‌یادداشت‌ها به ترتیب ارائه.
        var numbering = FsNoteNumbering.Number(
            versions.Select((v, i) => new FsNumberingStatement(
                v.TemplateCode,
                v.StatementType == FsStatementType.Note,
                i,
                v.NoteParentTemplateCode,
                v.NoteParentRowCode,
                v.Rows.Select(r => new FsNumberingRow(r.Code, r.ParentCode, r.OrderNo, r.RowType, r.NoteRef)).ToList()))
                .ToList(),
            request.NoteStartNo);

        run.NOTE_START_NO = request.NoteStartNo;

        var hash = new StringBuilder();
        var context = new SnapshotContext(run, values, byGroup, accountNames, request.IncludePrior, hash, numbering.RowNoteRefs);

        // ترتیب ارائه: صورت‌ها به ترتیب قالب، سپس یادداشت‌ها به ترتیب شماره.
        var presentation = versions.Where(v => v.StatementType != FsStatementType.Note)
            .Concat(numbering.NoteOrder.Select(code => versions.First(v => v.TemplateCode == code)))
            .ToList();

        foreach (var (v, order) in presentation.Select((v, i) => (v, i)))
        {
            var isNote = v.StatementType == FsStatementType.Note;
            var statement = new TB_FS_RUN_STATEMENT
            {
                ID = Guid.NewGuid(),
                RUN_ID = run.ID,
                VAHEDCODE = run.VAHEDCODE,
                TEMPLATE_ID = v.TemplateId,
                VERSION_ID = v.Id,
                TEMPLATE_CODE = v.TemplateCode,
                TITLE_FA = v.TemplateTitleFa,
                STATEMENT_TYPE = v.StatementType,
                ORDER_NO = (order + 1) * 10,
                VERSION_NO = v.VersionNo,
                VERSION_STATE = v.State,
                IS_NOTE = isNote,
            };

            if (isNote)
            {
                ApplyNoteCheck(statement, v, numbering, values, request.IncludePrior);
            }

            foreach (var r in v.Rows)
            {
                statement.TB_FS_RUN_ROWs.Add(BuildRow(statement.ID, v.TemplateCode, r, context));
            }

            run.TB_FS_RUN_STATEMENTs.Add(statement);
        }

        // بخش ۴۵-ه — کنترل‌ها، مقادیر دستی، اثر انگشت مانده‌ها، جایگزینی مبدأ.
        var outOfPeriod = await _balanceReadRepository.GetOutOfPeriodVouchersAsync(request.Year, unitCodes, request.MinDocLife, cancellationToken);
        // V-01/V-05/V-06 روی مانده‌های پیش از حذفیات (حذف نامتوازن را V-07 می‌گیرد، نه تراز آزمایشی).
        var checkBalances = all[FsColumns.Current]
            .Where(b => b.VahedCode != FsSyntheticAccounts.EliminationGroup)
            .GroupBy(b => b.AccCode, StringComparer.Ordinal)
            .Select(g => Sum(g.Key, g))
            .ToList();
        await AddChecksAsync(run, versions, values, checkBalances, scope, request.Framework, outOfPeriod, cancellationToken);
        AddExtraChecks(run, extra);
        AddManualValues(run, request.ManualValues);
        run.BALANCE_HASH = FsBalanceHash.Compute(raw);

        // ط-۳ — درخت واحد در لحظهٔ اجرا، کاربرگ به تفکیک گروه، و نتیجهٔ حذف‌ها.
        await AddRunExtrasAsync(run, versions, byGroup, unitCodes, extra, GroupOf, cancellationToken);

        if (sourceRun is not null)
        {
            run.SOURCE_RUN_ID = sourceRun.ID;
            sourceRun.STATE = FsRunState.Superseded;
            sourceRun.CHANGEUSERID = _currentUser.UserId;
            sourceRun.UPDATEDDATE = now;
            await _runRepository.AddActionAsync(new TB_FS_RUN_ACTION
            {
                ID = Guid.NewGuid(),
                RUN_ID = sourceRun.ID,
                VAHEDCODE = sourceRun.VAHEDCODE,
                ACTION = FsRunAction.Supersede,
                FROM_STATE = FsRunState.Draft,
                TO_STATE = FsRunState.Superseded,
                USERID = _currentUser.UserId,
                COMMENTS = $"جایگزین با اجرای شمارهٔ {run.RUN_NO}",
                CREATEDDATE = now,
            }, cancellationToken);
        }

        run.CONTENT_HASH = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hash.ToString()))).ToLowerInvariant();
        run.DURATION_MS = (int)Math.Min(stopwatch.ElapsedMilliseconds, int.MaxValue);

        await _runRepository.AddAsync(run, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return run.ID;
    }

    /// <summary>
    /// مقادیر دستی (علامت نمایشی) ⇒ علامت حسابداری برای موتور. هر مقدار باید مال ردیف «مقدار دستی»‌ای در
    /// یکی از نسخه‌های انتخاب‌شده باشد، وگرنه ۴۰۰.
    /// </summary>
    private static Dictionary<(string Stmt, string Row, string Col), decimal> BuildExternalValues(
        IReadOnlyList<FsTemplateVersionDetailDto> versions, IReadOnlyList<FsManualValueInput> manualValues)
    {
        var result = new Dictionary<(string, string, string), decimal>();

        foreach (var m in manualValues)
        {
            var row = versions.FirstOrDefault(v => v.TemplateCode == m.TemplateCode)?.Rows.FirstOrDefault(r => r.Code == m.RowCode);

            if (row is null || row.RowType != FsRowType.External)
            {
                throw Commands.Common.FsTemplateRules.Invalid(
                    "ManualValues", $"«{m.TemplateCode} / {m.RowCode}» ردیف «مقدار دستی» این اجرا نیست.");
            }

            var sign = row.NormalBalance == FsNormalBalance.Credit ? -1m : 1m;

            if (m.AmountCur is { } c)
            {
                result[(m.TemplateCode, m.RowCode, FsColumns.Current)] = sign * c;
            }

            if (m.AmountPrv is { } p)
            {
                result[(m.TemplateCode, m.RowCode, FsColumns.Prior)] = sign * p;
            }
        }

        return result;
    }

    /// <summary>
    /// کنترل‌های اجرا (<see cref="FsRunChecks"/>): قواعد داده‌ای فعالِ این مجموعه که برای واحد دیدنی‌اند —
    /// به‌ازای هر کد، قاعدهٔ نزدیک‌ترین مالک (مثل قالب‌ها) — به‌علاوهٔ V-01/V-05/V-06/V-08.
    /// </summary>
    private async Task AddChecksAsync(
        TB_FS_RUN run,
        IReadOnlyList<FsTemplateVersionDetailDto> versions,
        IReadOnlyDictionary<(string Stmt, string Row), IReadOnlyDictionary<string, FsRowValue>> values,
        IReadOnlyList<FsAccountBalance> currentBalances,
        FsUnitScope scope,
        FsFramework framework,
        FsOutOfPeriodVouchers outOfPeriod,
        CancellationToken cancellationToken)
    {
        var rules = (await _ruleRepository.GetAllAsync(framework, cancellationToken))
            .Where(r => r.IS_ACTIVE)
            .Select(r => (Rule: r, Priority: scope.PriorityOf(r.VAHEDCODE)))
            .Where(x => x.Priority is not null)
            .GroupBy(x => x.Rule.CODE, StringComparer.Ordinal)
            .Select(g => g.OrderBy(x => x.Priority).First().Rule)
            .Select(r => new FsCheckRuleInput(r.CODE, r.TITLE_FA, r.LEFT_EXPR, r.RIGHT_EXPR, r.TOLERANCE, r.SEVERITY))
            .ToList();

        var statements = versions
            .Select(v => new FsCheckStatement(
                v.TemplateCode,
                v.TemplateTitleFa,
                v.StatementType == FsStatementType.Note,
                v.Rows.Select(r => new FsCheckStatementRow(r.Code, r.TitleFa, r.RowType, r.Selector, r.ValueType, r.NormalBalance)).ToList()))
            .ToList();

        var notes = run.TB_FS_RUN_STATEMENTs
            .Where(s => s.IS_NOTE)
            .Select(s => new FsNoteCheckInput(
                s.TEMPLATE_CODE,
                s.NOTE_NO,
                s.TITLE_FA,
                s.PARENT_TEMPLATE_CODE is null ? null : $"{s.PARENT_TEMPLATE_CODE}/{s.PARENT_ROW_CODE}",
                s.CHECK_DIFF_CUR,
                s.CHECK_DIFF_PRV))
            .ToList();

        foreach (var c in FsRunChecks.Evaluate(statements, values, currentBalances, rules, notes, outOfPeriod))
        {
            run.TB_FS_RUN_CHECKs.Add(new TB_FS_RUN_CHECK
            {
                ID = Guid.NewGuid(),
                RUN_ID = run.ID,
                VAHEDCODE = run.VAHEDCODE,
                CODE = c.Code,
                TITLE_FA = Truncate(c.TitleFa, 500)!,
                SEVERITY = c.Severity,
                PASSED = c.Passed,
                MESSAGE = Truncate(c.Message, 1000),
                DIFFERENCE = c.Difference is { } d ? decimal.Round(d) : null,
                ROW_REF = Truncate(c.RowRef, 80),
            });
        }
    }

    private void AddManualValues(TB_FS_RUN run, IReadOnlyList<FsManualValueInput>? manualValues)
    {
        foreach (var m in manualValues ?? Array.Empty<FsManualValueInput>())
        {
            run.TB_FS_RUN_MANUALs.Add(new TB_FS_RUN_MANUAL
            {
                ID = Guid.NewGuid(),
                RUN_ID = run.ID,
                VAHEDCODE = run.VAHEDCODE,
                TEMPLATE_CODE = m.TemplateCode,
                ROW_CODE = m.RowCode,
                AMOUNT_CUR = m.AmountCur,
                AMOUNT_PRV = m.AmountPrv,
                REASON = m.Reason.Trim(),
                ADDUSERID = _currentUser.UserId,
                CREATEDDATE = run.CREATEDDATE,
            });
        }
    }

    private sealed record FsCheckNote(string Code, string TitleFa, FsCheckSeverity Severity, bool Passed, string? Message, decimal? Difference);

    private sealed record FsAdditions(
        IReadOnlyDictionary<string, string> EntityNames,
        IReadOnlyList<FsElimResult> Eliminations,
        IReadOnlyList<FsCheckNote> Notes);

    /// <summary>
    /// ط-۶ جریان نقد (طرف مقابل سند)، ط-۷ تجدید ارائه (تعدیلات سنواتی به ستون قبل)، ط-۵ شرکت‌های تابعه
    /// (تراز تسعیرشده)، ط-۴ حذف فی‌مابین — همه به‌صورت ماندهٔ افزوده در <paramref name="all"/>.
    /// </summary>
    private async Task<FsAdditions> ApplyAdditionsAsync(
        GenerateFsRunCommand request,
        FsUnitScope scope,
        int year,
        string fromDate,
        string toDate,
        IReadOnlyCollection<string> unitCodes,
        Dictionary<string, List<FsAccountBalance>> all,
        CancellationToken cancellationToken)
    {
        var notes = new List<FsCheckNote>();
        var settings = await _consolidation.GetSettingsAsync(request.Framework, cancellationToken);
        IReadOnlyList<Queries.AccountMapping.FsChartMoein>? chart = null;
        var priorYear = (year - 1).ToString(CultureInfo.InvariantCulture);
        var (priorFrom, priorTo) = Period(year - 1, request.ToMonth);

        async Task<HashSet<string>> CodesOf(string key, string selectorText)
        {
            chart ??= await _balanceReadRepository.GetChartMoeinsAsync(cancellationToken);

            if (!AccountSelector.TryParse(selectorText, out var selector, out var error))
            {
                throw new FsTemplateConflictException($"انتخاب‌گر «{key}» در تنظیمات مجموعه نامعتبر است: {error}");
            }

            return chart.Where(m => selector!.Match(m.AccCode) is not null).Select(m => m.AccCode).ToHashSet(StringComparer.Ordinal);
        }

        // ط-۶ — اثر نقدی: در هر سند دارای ردیف نقد، قرینهٔ ردیف‌های غیرنقد (ورود وجه مثبت).
        if (FsConsolidationEngine.Setting(settings, scope, request.Framework, "CASH_SELECTOR") is { } cashSelector)
        {
            var cashCodes = await CodesOf("CASH_SELECTOR", cashSelector);

            foreach (var col in all.Keys.ToList())
            {
                var current = col == FsColumns.Current;
                var lines = await _balanceReadRepository.GetTouchingVoucherLinesAsync(
                    current ? request.Year : priorYear, current ? fromDate : priorFrom, current ? toDate : priorTo,
                    unitCodes, request.MinDocLife, cashCodes, cancellationToken);
                all[col].AddRange(lines
                    .Where(l => !cashCodes.Contains(l.AccCode))
                    .Select(l => new FsAccountBalance(l.AccCode, l.AccName, 0, 0, 0, 0, l.VahedCode, -(l.Debtor - l.Creditor))));
            }
        }

        // ط-۷ — تجدید ارائه: اسناد سال جاری که به حساب تعدیلات سنواتی خورده‌اند، کامل به گردش ستون سال قبل.
        if (request.IncludePrior && request.PriorRestated && all.ContainsKey(FsColumns.Prior)
            && FsConsolidationEngine.Setting(settings, scope, request.Framework, "RESTATEMENT_SELECTOR") is { } restSelector)
        {
            var restCodes = await CodesOf("RESTATEMENT_SELECTOR", restSelector);
            var lines = await _balanceReadRepository.GetTouchingVoucherLinesAsync(
                request.Year, fromDate, toDate, unitCodes, request.MinDocLife, restCodes, cancellationToken);
            all[FsColumns.Prior].AddRange(lines.Select(l => new FsAccountBalance(l.AccCode, l.AccName, 0, 0, l.Debtor, l.Creditor, l.VahedCode)));
            notes.Add(new FsCheckNote(
                "RST", "تجدید ارائه: تعدیلات سنواتی سال جاری در ستون سال قبل", FsCheckSeverity.Info, true,
                $"{lines.Count} ردیف (معین/واحد) از اسناد تعدیلات سنواتی به ستون سال قبل افزوده شد.", null));
        }

        // ط-۵ — شرکت‌های تابعهٔ واحد اجرا (تراز Excel، تسعیر، سهم غیرکنترلی).
        var entityNames = new Dictionary<string, string>(StringComparer.Ordinal);

        if (request.IncludeEntities)
        {
            var entities = (await _consolidation.GetEntitiesAsync(cancellationToken))
                .Where(e => e.IS_ACTIVE && e.VAHEDCODE == request.VahedCode)
                .ToList();

            if (entities.Count == 0)
            {
                throw new FsTemplateConflictException("برای این واحد شرکت تابعهٔ فعالی تعریف نشده است؛ گزینهٔ «تلفیق با شرکت‌های تابعه» را بردارید.");
            }

            if (entities.FirstOrDefault(e => scope.Names.ContainsKey(e.CODE)) is { } clash)
            {
                throw new FsTemplateConflictException($"کد شرکت تابعهٔ «{clash.TITLE_FA}» ({clash.CODE}) با کد یک واحد سازمان یکی است؛ کد شرکت را عوض کنید.");
            }

            var ids = entities.Select(e => e.ID).ToList();

            foreach (var col in all.Keys.ToList())
            {
                var colYear = col == FsColumns.Current ? request.Year : priorYear;
                var tbs = await _consolidation.GetEntityTbAsync(ids, colYear, request.ToMonth, cancellationToken);
                var rates = await _consolidation.GetRatesAsync(ids, colYear, request.ToMonth, cancellationToken);

                foreach (var e in entities)
                {
                    var rows = tbs.Where(t => t.ENTITY_ID == e.ID).ToList();

                    if (rows.Count == 0)
                    {
                        if (col == FsColumns.Current)
                        {
                            throw new FsTemplateConflictException($"تراز شرکت «{e.TITLE_FA}» برای سال {colYear} تا ماه {request.ToMonth} وارد نشده است.");
                        }

                        notes.Add(new FsCheckNote("ENT", $"تراز سال قبل شرکت «{e.TITLE_FA}»", FsCheckSeverity.Warning, false,
                            $"تراز سال {colYear} وارد نشده؛ ستون سال قبل بدون این شرکت است.", null));
                        continue;
                    }

                    try
                    {
                        all[col].AddRange(FsConsolidationEngine.Translate(new FsEntityInput(e, rows, rates.FirstOrDefault(r => r.ENTITY_ID == e.ID))));
                    }
                    catch (FsEngineException ex)
                    {
                        throw new FsTemplateConflictException(ex.Message);
                    }

                    entityNames[e.CODE] = e.TITLE_FA;
                }
            }
        }

        // ط-۴ — حذف فی‌مابین در صورت ترکیبی یا تلفیقی؛ برای هر کد، قاعدهٔ نزدیک‌ترین مالک.
        IReadOnlyList<FsElimResult> eliminations = [];

        if (request.IncludeSubUnits || request.IncludeEntities)
        {
            var rules = (await _consolidation.GetElimRulesAsync(request.Framework, cancellationToken))
                .Where(r => r.IS_ACTIVE)
                .Select(r => (Rule: r, Priority: scope.PriorityOf(r.VAHEDCODE)))
                .Where(x => x.Priority is not null)
                .GroupBy(x => x.Rule.CODE, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderBy(x => x.Priority).First().Rule)
                .OrderBy(r => r.CODE, StringComparer.Ordinal)
                .ToList();

            foreach (var col in all.Keys.ToList())
            {
                if (rules.Count == 0)
                {
                    break;
                }

                var (adjustments, results) = FsConsolidationEngine.Eliminate(all[col], rules);
                all[col].AddRange(adjustments);

                if (col == FsColumns.Current)
                {
                    eliminations = results;
                }
            }
        }

        return new FsAdditions(entityNames, eliminations, notes);
    }

    /// <summary>V-07 برای هر قاعدهٔ حذف (مسدودکننده) و یادداشت‌های افزوده (تجدید ارائه، تراز شرکت‌ها).</summary>
    private static void AddExtraChecks(TB_FS_RUN run, FsAdditions extra)
    {
        static string StatusText(int s) => s switch
        {
            1 => "تطبیق کامل",
            2 => "در آستانهٔ مجاز",
            4 => "طرف مقابل ندارد",
            _ => "عدم تطبیق",
        };

        var checks = extra.Eliminations
            .Select(e => new FsCheckNote(
                "V-07",
                $"حذف فی‌مابین {e.Code}: {e.TitleFa}",
                FsCheckSeverity.Blocking,
                e.Status is 1 or 2,
                $"{StatusText(e.Status)} — سوی اول {e.Left:#,0}، سوی دوم {e.Right:#,0}، اختلاف {e.Difference:#,0} ریال.",
                e.Difference))
            .Concat(extra.Notes);

        foreach (var c in checks)
        {
            run.TB_FS_RUN_CHECKs.Add(new TB_FS_RUN_CHECK
            {
                ID = Guid.NewGuid(),
                RUN_ID = run.ID,
                VAHEDCODE = run.VAHEDCODE,
                CODE = c.Code,
                TITLE_FA = Truncate(c.TitleFa, 500)!,
                SEVERITY = c.Severity,
                PASSED = c.Passed,
                MESSAGE = Truncate(c.Message, 1000),
                DIFFERENCE = c.Difference,
            });
        }
    }

    /// <summary>
    /// ط-۳ — (۱) درخت واحدها و شرکت‌های دامنه در لحظهٔ اجرا؛ (۲) کاربرگ: موتور برای هر گروه جداگانه (بدون مقادیر
    /// دستی) و مبلغ هر ردیف مقداری؛ (۳) نتیجهٔ حذف‌ها. کاربرگ فقط وقتی بیش از یک گروه هست.
    /// </summary>
    private async Task AddRunExtrasAsync(
        TB_FS_RUN run,
        IReadOnlyList<FsTemplateVersionDetailDto> versions,
        IReadOnlyDictionary<string, Dictionary<string, Dictionary<string, FsAccountBalance>>> byGroup,
        IReadOnlyCollection<string> unitCodes,
        FsAdditions extra,
        Func<string?, string> groupOf,
        CancellationToken cancellationToken)
    {
        var allUnits = await _unitAccess.GetAllUnitsAsync(cancellationToken);
        var byId = allUnits.ToDictionary(u => u.Id);
        var units = allUnits
            .Where(u => unitCodes.Contains(u.VahedCode))
            .Select(u => new TB_FS_RUN_UNIT
            {
                ID = Guid.NewGuid(),
                RUN_ID = run.ID,
                VAHEDCODE = run.VAHEDCODE,
                UNIT_CODE = u.VahedCode,
                UNIT_NAME = Truncate(u.VahedName, 200),
                PARENT_CODE = u.ParentId is { } pid && byId.TryGetValue(pid, out var p) ? p.VahedCode : null,
                GROUP_CODE = groupOf(u.VahedCode),
                KIND = 1,
            })
            .Concat(extra.EntityNames.Select(e => new TB_FS_RUN_UNIT
            {
                ID = Guid.NewGuid(),
                RUN_ID = run.ID,
                VAHEDCODE = run.VAHEDCODE,
                UNIT_CODE = e.Key,
                UNIT_NAME = Truncate(e.Value, 200),
                PARENT_CODE = run.VAHEDCODE,
                GROUP_CODE = e.Key,
                KIND = 2,
            }))
            .ToList();

        var groups = byGroup.Values
            .SelectMany(accs => accs.Values.SelectMany(g => g.Keys))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(g => g, StringComparer.Ordinal)
            .ToList();

        var rowGroups = new List<TB_FS_RUN_ROW_GROUP>();

        if (groups.Count > 1)
        {
            var engineStatements = versions
                .Select(v => new FsEngineStatement(
                    v.TemplateCode,
                    v.Rows.Select(r => new FsEngineRow(r.Code, r.OrderNo, r.RowType, r.Selector, r.ValueType, r.Formula)).ToList()))
                .ToList();
            var rowIds = run.TB_FS_RUN_STATEMENTs
                .SelectMany(s => s.TB_FS_RUN_ROWs.Select(r => (s.TEMPLATE_CODE, r.ROW_CODE, r.ID, r.ROW_TYPE)))
                .Where(x => x.ROW_TYPE is FsRowType.Account or FsRowType.Formula or FsRowType.External)
                .ToList();

            foreach (var g in groups)
            {
                var balances = byGroup.ToDictionary(
                    kv => kv.Key,
                    kv => (IReadOnlyList<FsAccountBalance>)kv.Value
                        .Where(acc => acc.Value.ContainsKey(g))
                        .Select(acc => acc.Value[g])
                        .ToList(),
                    StringComparer.Ordinal);

                IReadOnlyDictionary<(string Stmt, string Row), IReadOnlyDictionary<string, FsRowValue>> values;

                try
                {
                    values = FsStatementEngine.Compute(engineStatements, balances);
                }
                catch (Exception ex) when (ex is FsEngineException or FsExpressionException or KeyNotFoundException)
                {
                    continue;
                }

                foreach (var (stmt, rowCode, rowId, _) in rowIds)
                {
                    if (!values.TryGetValue((stmt, rowCode), out var byCol))
                    {
                        continue;
                    }

                    decimal? cur = byCol.GetValueOrDefault(FsColumns.Current)?.Amount is { } c ? decimal.Round(c) : null;
                    decimal? prv = run.HAS_PRIOR && byCol.GetValueOrDefault(FsColumns.Prior)?.Amount is { } pr ? decimal.Round(pr) : null;

                    if ((cur ?? 0) == 0 && (prv ?? 0) == 0)
                    {
                        continue;
                    }

                    rowGroups.Add(new TB_FS_RUN_ROW_GROUP
                    {
                        ID = Guid.NewGuid(),
                        RUN_ID = run.ID,
                        VAHEDCODE = run.VAHEDCODE,
                        RUN_ROW_ID = rowId,
                        GROUP_CODE = g,
                        AMOUNT_CUR = cur,
                        AMOUNT_PRV = prv,
                    });
                }
            }
        }

        var elims = extra.Eliminations.Select(e => new TB_FS_RUN_ELIM
        {
            ID = Guid.NewGuid(),
            RUN_ID = run.ID,
            VAHEDCODE = run.VAHEDCODE,
            RULE_CODE = e.Code,
            TITLE_FA = Truncate(e.TitleFa, 500)!,
            LEFT_AMOUNT = e.Left,
            RIGHT_AMOUNT = e.Right,
            DIFFERENCE = e.Difference,
            STATUS = e.Status,
        });

        await _consolidation.AddRunExtrasAsync(units, rowGroups, elims, cancellationToken);
    }

    private static string? Truncate(string? s, int max) => s is null || s.Length <= max ? s : s[..max];

    /// <summary>ابتدای سال تا آخر ماه؛ «31» برای همهٔ ماه‌ها درست است چون مقایسه رشته‌ای و شامل است.</summary>
    private static (string From, string To) Period(int year, int toMonth)
        => ($"{year:0000}0101", $"{year:0000}{toMonth:00}31");

    private sealed record SnapshotContext(
        TB_FS_RUN Run,
        IReadOnlyDictionary<(string Stmt, string Row), IReadOnlyDictionary<string, FsRowValue>> Values,
        IReadOnlyDictionary<string, Dictionary<string, Dictionary<string, FsAccountBalance>>> ByGroup,
        IReadOnlyDictionary<string, string?> AccountNames,
        bool IncludePrior,
        StringBuilder Hash,
        IReadOnlyDictionary<(string Stmt, string Row), string> NoteRefs);

    /// <summary>
    /// شماره، والد و کنترل V-08 یک یادداشت: جمع یادداشت (ردیف جمع تعیین‌شده، یا آخرین ردیف مقداری) منهای
    /// ردیف صورت والد، برای هر ستون — هر دو با علامت حسابداری، پس ماهیت‌ها مقایسه را به هم نمی‌زند.
    /// </summary>
    private static void ApplyNoteCheck(
        TB_FS_RUN_STATEMENT statement,
        FsTemplateVersionDetailDto note,
        FsNoteNumberingResult numbering,
        IReadOnlyDictionary<(string Stmt, string Row), IReadOnlyDictionary<string, FsRowValue>> values,
        bool includePrior)
    {
        statement.NOTE_NO = numbering.NoteNumbers.GetValueOrDefault(note.TemplateCode);

        var totalRow = note.NoteTotalRowCode
            ?? note.Rows.Where(r => r.RowType is FsRowType.Account or FsRowType.Formula or FsRowType.External)
                .OrderBy(r => r.OrderNo).LastOrDefault()?.Code;
        statement.TOTAL_ROW_CODE = totalRow;

        if (!numbering.ResolvedParents.TryGetValue(note.TemplateCode, out var parent))
        {
            return;
        }

        statement.PARENT_TEMPLATE_CODE = parent.Stmt;
        statement.PARENT_ROW_CODE = parent.Row;

        if (totalRow is null || !values.TryGetValue((note.TemplateCode, totalRow), out var noteValues)
            || !values.TryGetValue(parent, out var parentValues))
        {
            return;
        }

        decimal? Diff(string col) =>
            noteValues.GetValueOrDefault(col)?.Amount is { } n && parentValues.GetValueOrDefault(col)?.Amount is { } p
                ? decimal.Round(n) - decimal.Round(p)
                : null;

        statement.CHECK_DIFF_CUR = Diff(FsColumns.Current);
        statement.CHECK_DIFF_PRV = includePrior ? Diff(FsColumns.Prior) : null;
    }

    private static TB_FS_RUN_ROW BuildRow(Guid statementId, string templateCode, FsTemplateRowDto r, SnapshotContext ctx)
    {
        var byCol = ctx.Values[(templateCode, r.Code)];
        var cur = byCol.GetValueOrDefault(FsColumns.Current);
        var prv = ctx.IncludePrior ? byCol.GetValueOrDefault(FsColumns.Prior) : null;

        var row = new TB_FS_RUN_ROW
        {
            ID = Guid.NewGuid(),
            RUN_STATEMENT_ID = statementId,
            RUN_ID = ctx.Run.ID,
            VAHEDCODE = ctx.Run.VAHEDCODE,
            ROW_CODE = r.Code,
            PARENT_CODE = r.ParentCode,
            ORDER_NO = r.OrderNo,
            ROW_TYPE = r.RowType,
            TITLE_FA = r.TitleFa,
            TITLE_EN = r.TitleEn,
            NOTE_REF = ctx.NoteRefs.GetValueOrDefault((templateCode, r.Code)) ?? r.NoteRef,
            NORMAL_BALANCE = r.NormalBalance,
            SELECTOR = r.Selector,
            VALUE_TYPE = r.ValueType,
            FORMULA = r.Formula,
            FORMAT_JSON = FsRowFormat.ToJson(r.Format),
            IS_DRILLABLE = r.IsDrillable,
            AMOUNT_CUR = cur?.Amount is { } c ? decimal.Round(c) : null,
            AMOUNT_PRV = prv?.Amount is { } p ? decimal.Round(p) : null,
        };

        ctx.Hash.Append(templateCode).Append('|').Append(r.Code).Append('|')
            .Append(row.AMOUNT_CUR?.ToString(CultureInfo.InvariantCulture)).Append('|')
            .Append(row.AMOUNT_PRV?.ToString(CultureInfo.InvariantCulture)).Append('\n');

        if (r.RowType != FsRowType.Account)
        {
            return row;
        }

        // معین‌هایی که موتور در این ردیف حساب کرده (پس از فیلتر [D]/[C] روی جمع واحدها)؛ سهم هر
        // زیرواحد از همان معین با همان VALUE_TYPE — جمع سهم‌ها = مبلغ معین در ردیف.
        var accounts = (cur?.Accounts?.Keys ?? Enumerable.Empty<string>())
            .Union(prv?.Accounts?.Keys ?? Enumerable.Empty<string>(), StringComparer.Ordinal)
            .OrderBy(a => a, StringComparer.Ordinal);

        foreach (var acc in accounts)
        {
            var curGroups = cur?.Accounts?.ContainsKey(acc) == true ? GroupsOf(ctx, FsColumns.Current, acc) : null;
            var prvGroups = prv?.Accounts?.ContainsKey(acc) == true ? GroupsOf(ctx, FsColumns.Prior, acc) : null;

            var groups = (curGroups?.Keys ?? Enumerable.Empty<string>())
                .Union(prvGroups?.Keys ?? Enumerable.Empty<string>(), StringComparer.Ordinal)
                .OrderBy(g => g, StringComparer.Ordinal);

            foreach (var group in groups)
            {
                decimal? curAmount = curGroups?.TryGetValue(group, out var cb) == true ? decimal.Round(FsStatementEngine.AmountOf(cb!, r.ValueType)) : null;
                decimal? prvAmount = prvGroups?.TryGetValue(group, out var pb) == true ? decimal.Round(FsStatementEngine.AmountOf(pb!, r.ValueType)) : null;

                if ((curAmount ?? 0) == 0 && (prvAmount ?? 0) == 0)
                {
                    continue;
                }

                row.TB_FS_RUN_ACCOUNTs.Add(new TB_FS_RUN_ACCOUNT
                {
                    ID = Guid.NewGuid(),
                    RUN_ROW_ID = row.ID,
                    RUN_ID = ctx.Run.ID,
                    VAHEDCODE = ctx.Run.VAHEDCODE,
                    SOURCE_VAHEDCODE = group,
                    ACCCODE = acc,
                    ACCNAME = ctx.AccountNames.GetValueOrDefault(acc),
                    AMOUNT_CUR = curAmount,
                    AMOUNT_PRV = prvAmount,
                });
            }
        }

        return row;
    }

    private static Dictionary<string, FsAccountBalance>? GroupsOf(SnapshotContext ctx, string column, string acc)
        => ctx.ByGroup.TryGetValue(column, out var accs) && accs.TryGetValue(acc, out var groups) ? groups : null;
}
