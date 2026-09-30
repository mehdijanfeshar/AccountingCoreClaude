using System.Globalization;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.FinancialStatements.Engine;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.FinancialStatements.Queries.Drill;

public sealed class GetFsRunRowAccountsQueryHandler : IRequestHandler<GetFsRunRowAccountsQuery, IReadOnlyList<FsDrillAccountDto>?>
{
    private readonly IFsRunRepository _runRepository;

    public GetFsRunRowAccountsQueryHandler(IFsRunRepository runRepository)
    {
        _runRepository = runRepository;
    }

    public async Task<IReadOnlyList<FsDrillAccountDto>?> Handle(GetFsRunRowAccountsQuery request, CancellationToken cancellationToken)
    {
        var target = await _runRepository.GetDrillTargetAsync(request.RunId, request.RowId, request.VahedCode, cancellationToken);

        return target is null
            ? null
            : await _runRepository.GetRowAccountsAsync(request.RunId, request.RowId, request.VahedCode, cancellationToken);
    }
}

public sealed class GetFsRunRowUnitsQueryHandler : IRequestHandler<GetFsRunRowUnitsQuery, IReadOnlyList<FsDrillUnitDto>?>
{
    private readonly IFsRunRepository _runRepository;
    private readonly IFsUnitScopeProvider _scopes;

    public GetFsRunRowUnitsQueryHandler(IFsRunRepository runRepository, IFsUnitScopeProvider scopes)
    {
        _runRepository = runRepository;
        _scopes = scopes;
    }

    public async Task<IReadOnlyList<FsDrillUnitDto>?> Handle(GetFsRunRowUnitsQuery request, CancellationToken cancellationToken)
    {
        var target = await _runRepository.GetDrillTargetAsync(request.RunId, request.RowId, request.VahedCode, cancellationToken);

        if (target is null)
        {
            return null;
        }

        var units = await _runRepository.GetRowUnitsAsync(request.RunId, request.RowId, request.VahedCode, request.AccCode, cancellationToken);
        var scope = await _scopes.GetAsync(request.VahedCode, cancellationToken);

        return units.Select(u => u with { VahedName = scope.Names.GetValueOrDefault(u.VahedCode) }).ToList();
    }
}

/// <summary>
/// سطح «سند». قفل‌های دسترسی: اجرا/ردیف باید مال واحد هدر باشد؛ معین و زیرواحد باید در ترکیب همین ردیف
/// در Snapshot باشند (وگرنه ۴۰۴) — پس این endpoint راهی برای خواندن اسناد معین یا واحد دلخواه نیست.
/// </summary>
public sealed class GetFsRunRowVouchersQueryHandler : IRequestHandler<GetFsRunRowVouchersQuery, FsDrillVoucherPageDto?>
{
    private readonly IFsRunRepository _runRepository;
    private readonly IFsBalanceReadRepository _balanceReadRepository;
    private readonly IFsUnitScopeProvider _scopes;

    public GetFsRunRowVouchersQueryHandler(
        IFsRunRepository runRepository,
        IFsBalanceReadRepository balanceReadRepository,
        IFsUnitScopeProvider scopes)
    {
        _runRepository = runRepository;
        _balanceReadRepository = balanceReadRepository;
        _scopes = scopes;
    }

    public async Task<FsDrillVoucherPageDto?> Handle(GetFsRunRowVouchersQuery request, CancellationToken cancellationToken)
    {
        var target = await _runRepository.GetDrillTargetAsync(request.RunId, request.RowId, request.VahedCode, cancellationToken);

        if (target is null || target.RowType != FsRowType.Account || (request.Column == FsColumns.Prior && !target.HasPrior))
        {
            return null;
        }

        var shares = await _runRepository.GetRowUnitsAsync(request.RunId, request.RowId, request.VahedCode, request.AccCode, cancellationToken);

        if (shares.Count == 0 || (request.Unit is not null && shares.All(s => s.VahedCode != request.Unit)))
        {
            return null;
        }

        List<string> units;

        if (!target.IncludeSubUnits)
        {
            units = new List<string> { target.VahedCode };
        }
        else
        {
            var scope = await _scopes.GetAsync(target.VahedCode, cancellationToken);
            units = request.Unit is null
                ? scope.Accessible.ToList()
                : scope.Accessible.Where(c => scope.GroupOf(c) == request.Unit).ToList();
        }

        var year = target.Year;
        var fromDate = target.FromDate;
        var toDate = target.ToDate;

        if (request.Column == FsColumns.Prior)
        {
            var prior = (int.Parse(target.Year, CultureInfo.InvariantCulture) - 1).ToString("0000", CultureInfo.InvariantCulture);
            year = prior;
            fromDate = prior + target.FromDate[4..];
            toDate = prior + target.ToDate[4..];
        }

        var window = target.ValueType switch
        {
            FsValueType.Opening => FsDrillWindow.Opening,
            FsValueType.Movement or FsValueType.Debit or FsValueType.Credit => FsDrillWindow.Period,
            _ => FsDrillWindow.All,
        };

        return await _balanceReadRepository.GetVoucherLinesAsync(
            year, fromDate, toDate, units, target.MinDocLife, request.AccCode, window, request.PageNumber, request.PageSize, cancellationToken);
    }
}

public sealed class GetFsRunExcelQueryHandler : IRequestHandler<GetFsRunExcelQuery, FsFileDto?>
{
    private readonly IFsRunRepository _runRepository;
    private readonly IFsExcelExporter _exporter;

    public GetFsRunExcelQueryHandler(IFsRunRepository runRepository, IFsExcelExporter exporter)
    {
        _runRepository = runRepository;
        _exporter = exporter;
    }

    public async Task<FsFileDto?> Handle(GetFsRunExcelQuery request, CancellationToken cancellationToken)
    {
        var detail = await _runRepository.GetDetailAsync(request.RunId, request.VahedCode, cancellationToken);

        if (detail is null)
        {
            return null;
        }

        return new FsFileDto(
            $"FS-{detail.Run.RunNo}-{detail.Run.Year}-{detail.Run.ToMonth:00}.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _exporter.Export(detail));
    }
}
