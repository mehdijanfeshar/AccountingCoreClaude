using Accounting.Application.Common.Interfaces;
using Accounting.Application.FinancialStatements.Engine;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.FinancialStatements.Commands.ApplyFsAccountMapping;

public sealed class ApplyFsAccountMappingCommandHandler : IRequestHandler<ApplyFsAccountMappingCommand, FsMappingApplyResultDto>
{
    private readonly IFsTemplateReadRepository _templateReadRepository;
    private readonly IFsTemplateRepository _repository;
    private readonly IFsBalanceReadRepository _balanceReadRepository;
    private readonly IFsUnitScopeProvider _scopes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public ApplyFsAccountMappingCommandHandler(
        IFsTemplateReadRepository templateReadRepository,
        IFsTemplateRepository repository,
        IFsBalanceReadRepository balanceReadRepository,
        IFsUnitScopeProvider scopes,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _templateReadRepository = templateReadRepository;
        _repository = repository;
        _balanceReadRepository = balanceReadRepository;
        _scopes = scopes;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<FsMappingApplyResultDto> Handle(ApplyFsAccountMappingCommand request, CancellationToken cancellationToken)
    {
        var scope = await _scopes.GetAsync(request.VahedCode, cancellationToken);
        var versions = (await _templateReadRepository.GetVersionsForRunAsync(request.Framework, request.Year, true, scope, cancellationToken))
            .ToDictionary(v => v.TemplateCode, StringComparer.OrdinalIgnoreCase);
        var chart = (await _balanceReadRepository.GetChartMoeinsAsync(cancellationToken))
            .Select(m => m.AccCode)
            .ToHashSet(StringComparer.Ordinal);

        var rowsByVersion = new Dictionary<Guid, IReadOnlyList<TB_FS_TEMPLATE_ROW>>();
        var results = new List<FsMappingApplyItemResult>(request.Items.Count);
        var touched = new HashSet<TB_FS_TEMPLATE_ROW>();

        foreach (var item in request.Items)
        {
            var acc = FsText.NormalizeDigits(item.AccCode).Trim();
            var templateCode = item.TemplateCode.Trim();
            var rowCode = item.RowCode.Trim();
            FsMappingApplyItemResult Result(string status, string? message = null) => new(acc, templateCode, rowCode, status, message);

            if (!chart.Contains(acc))
            {
                results.Add(Result("error", "این معین در کدینگ نیست یا حذف شده است."));
                continue;
            }

            if (!versions.TryGetValue(templateCode, out var version))
            {
                results.Add(Result("error", "این قالب در مجموعهٔ انتخاب‌شده برای این واحد نیست."));
                continue;
            }

            if (!version.CanEdit)
            {
                results.Add(Result("error", "این قالب از این واحد قابل تغییر نیست."));
                continue;
            }

            if (version.State != FsTemplateVersionState.Draft)
            {
                results.Add(Result("error", "قالب پیش‌نویس ندارد؛ اول از صفحهٔ قالب «پیش‌نویس جدید» بسازید."));
                continue;
            }

            if (!rowsByVersion.TryGetValue(version.Id, out var rows))
            {
                rows = await _repository.GetRowsForUpdateAsync(version.Id, cancellationToken);
                rowsByVersion[version.Id] = rows;
            }

            var target = rows.FirstOrDefault(r => string.Equals(r.CODE, rowCode, StringComparison.OrdinalIgnoreCase));

            if (target is null || target.ROW_TYPE != FsRowType.Account)
            {
                results.Add(Result("error", target is null ? "ردیف در قالب نیست." : "ردیف مقصد باید از نوع «حساب» باشد."));
                continue;
            }

            var changed = TryAdd(target, acc, out var addError);

            if (addError is not null)
            {
                results.Add(Result("error", addError));
                continue;
            }

            foreach (var other in rows.Where(r => r.ID != target.ID && r.ROW_TYPE == FsRowType.Account))
            {
                changed |= Remove(other, acc);
            }

            results.Add(Result(changed ? "applied" : "unchanged"));
        }

        if (!request.DryRun && touched.Count > 0)
        {
            var now = DateTime.UtcNow;

            foreach (var r in touched)
            {
                r.CHANGEUSERID = _currentUser.UserId;
                r.UPDATEDDATE = now;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new FsMappingApplyResultDto(
            results.Count(r => r.Status == "applied"),
            results.Count(r => r.Status == "error"),
            request.DryRun,
            results);

        // true = انتخاب‌گر عوض شد؛ خطا در error.
        bool TryAdd(TB_FS_TEMPLATE_ROW row, string code, out string? error)
        {
            error = null;
            var selector = Parse(row.SELECTOR);

            if (selector?.Match(code) is { Side: SelectorBalanceSide.Any })
            {
                return false;
            }

            var terms = selector?.Terms
                .Where(t => !(t.Exclude && t.Kind == SelectorTermKind.Exact && t.From == code))
                .ToList() ?? [];

            if (terms.Any(t => t.Exclude && t.MatchesCode(code)))
            {
                error = "انتخاب‌گر ردیف مقصد این معین را با استثنای گسترده کنار گذاشته؛ دستی اصلاح کنید.";
                return false;
            }

            terms.Add(new SelectorTerm(false, SelectorTermKind.Exact, code, null, SelectorBalanceSide.Any));
            Set(row, terms);
            return true;
        }

        // ردیف‌های [D]/[C] عمداً دست نمی‌خورند (تفکیک مانده بدهکار/بستانکار).
        bool Remove(TB_FS_TEMPLATE_ROW row, string code)
        {
            var selector = Parse(row.SELECTOR);

            if (selector?.Match(code) is not { Side: SelectorBalanceSide.Any })
            {
                return false;
            }

            var terms = selector.Terms
                .Where(t => !(!t.Exclude && t.Kind == SelectorTermKind.Exact && t.From == code))
                .ToList();

            if (terms.Any(t => !t.Exclude && t.MatchesCode(code)))
            {
                terms.Add(new SelectorTerm(true, SelectorTermKind.Exact, code, null, SelectorBalanceSide.Any));
            }

            Set(row, terms);
            return true;
        }

        void Set(TB_FS_TEMPLATE_ROW row, List<SelectorTerm> terms)
        {
            row.SELECTOR = terms.Any(t => !t.Exclude) ? AccountSelector.FromTerms(terms).ToString() : null;
            touched.Add(row);
        }
    }

    private static AccountSelector? Parse(string? text)
        => string.IsNullOrWhiteSpace(text) ? null : AccountSelector.TryParse(text, out var s, out _) ? s : null;
}
