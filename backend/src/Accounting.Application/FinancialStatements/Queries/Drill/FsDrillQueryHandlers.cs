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
    private readonly FsDrillVoucherReader _reader;

    public GetFsRunRowVouchersQueryHandler(FsDrillVoucherReader reader)
    {
        _reader = reader;
    }

    public Task<FsDrillVoucherPageDto?> Handle(GetFsRunRowVouchersQuery request, CancellationToken cancellationToken)
        => _reader.ReadAsync(
            request.RunId, request.RowId, request.VahedCode, request.AccCode, request.Unit, request.Column,
            request.PageNumber, request.PageSize, cancellationToken);
}

/// <summary>
/// منطق مشترک سطح «سند» Drill-down (صفحهٔ Drill و خروجی Excel آن، بخش ۴۵-د): قفل‌های دسترسی (اجرا/ردیف مال
/// واحد هدر؛ معین و زیرواحد در ترکیب همین ردیف)، دامنهٔ واحدها، دورهٔ ستون، و بخش دورهٔ <c>VALUE_TYPE</c>.
/// <see langword="null"/> = ۴۰۴.
/// </summary>
public sealed class FsDrillVoucherReader
{
    private readonly IFsRunRepository _runRepository;
    private readonly IFsBalanceReadRepository _balanceReadRepository;
    private readonly IFsUnitScopeProvider _scopes;

    public FsDrillVoucherReader(
        IFsRunRepository runRepository,
        IFsBalanceReadRepository balanceReadRepository,
        IFsUnitScopeProvider scopes)
    {
        _runRepository = runRepository;
        _balanceReadRepository = balanceReadRepository;
        _scopes = scopes;
    }

    public async Task<FsDrillVoucherPageDto?> ReadAsync(
        Guid runId,
        Guid rowId,
        string vahedCode,
        string accCode,
        string? unit,
        string column,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var target = await _runRepository.GetDrillTargetAsync(runId, rowId, vahedCode, cancellationToken);

        if (target is null || target.RowType != FsRowType.Account || (column == FsColumns.Prior && !target.HasPrior))
        {
            return null;
        }

        var shares = await _runRepository.GetRowUnitsAsync(runId, rowId, vahedCode, accCode, cancellationToken);

        if (shares.Count == 0 || (unit is not null && shares.All(s => s.VahedCode != unit)))
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
            units = unit is null
                ? scope.Accessible.ToList()
                : scope.Accessible.Where(c => scope.GroupOf(c) == unit).ToList();
        }

        var year = target.Year;
        var fromDate = target.FromDate;
        var toDate = target.ToDate;

        if (column == FsColumns.Prior)
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
            year, fromDate, toDate, units, target.MinDocLife, accCode, window, pageNumber, pageSize, cancellationToken);
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

public sealed class GetFsRunRowDrillExcelQueryHandler : IRequestHandler<GetFsRunRowDrillExcelQuery, FsFileDto?>
{
    /// <summary>سقف ردیف‌های سند در یک فایل — بیشتر از این برای Excel دستی بی‌معناست.</summary>
    private const int MaxVoucherLines = 50_000;

    private readonly IFsRunRepository _runRepository;
    private readonly IFsUnitScopeProvider _scopes;
    private readonly FsDrillVoucherReader _voucherReader;
    private readonly IFsExcelExporter _exporter;

    public GetFsRunRowDrillExcelQueryHandler(
        IFsRunRepository runRepository,
        IFsUnitScopeProvider scopes,
        FsDrillVoucherReader voucherReader,
        IFsExcelExporter exporter)
    {
        _runRepository = runRepository;
        _scopes = scopes;
        _voucherReader = voucherReader;
        _exporter = exporter;
    }

    public async Task<FsFileDto?> Handle(GetFsRunRowDrillExcelQuery request, CancellationToken cancellationToken)
    {
        var target = await _runRepository.GetDrillTargetAsync(request.RunId, request.RowId, request.VahedCode, cancellationToken);

        if (target is null || target.RowType != FsRowType.Account)
        {
            return null;
        }

        var accounts = await _runRepository.GetRowAccountsAsync(request.RunId, request.RowId, request.VahedCode, cancellationToken);
        var scope = await _scopes.GetAsync(request.VahedCode, cancellationToken);
        var units = (await _runRepository.GetRowUnitsAsync(request.RunId, request.RowId, request.VahedCode, request.AccCode, cancellationToken))
            .Select(u => u with { VahedName = scope.Names.GetValueOrDefault(u.VahedCode) })
            .ToList();

        FsDrillVoucherPageDto? vouchers = null;

        if (request.AccCode is not null)
        {
            vouchers = await _voucherReader.ReadAsync(
                request.RunId, request.RowId, request.VahedCode, request.AccCode, request.Unit, request.Column,
                1, MaxVoucherLines, cancellationToken);

            if (vouchers is null)
            {
                return null;
            }
        }

        // ماهیت ردیف از Snapshot — برای مبلغ نمایشی معین‌ها و واحدها.
        var detail = await _runRepository.GetDetailAsync(request.RunId, request.VahedCode, cancellationToken);
        var normalBalance = detail?.Statements.SelectMany(s => s.Rows).FirstOrDefault(r => r.Id == request.RowId)?.NormalBalance;

        return new FsFileDto(
            $"FS-Drill-{target.RowCode}{(request.AccCode is null ? string.Empty : "-" + request.AccCode)}.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _exporter.ExportDrill(target, normalBalance, accounts, units, request.AccCode, vouchers));
    }
}
