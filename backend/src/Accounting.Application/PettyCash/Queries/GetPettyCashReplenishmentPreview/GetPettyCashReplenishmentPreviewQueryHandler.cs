using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashReplenishmentPreview;

/// <summary>
/// Combines <see cref="IPettyCashFundReadRepository.GetByIdAsync"/> (سقف/موجودی نقد/تأییدشده/در
/// جریان — already includes بخش ۳-الف's extended balance terms) with
/// <see cref="IPettyCashReplenishmentReadRepository.GetUnlinkedApprovedGroupedAsync"/> (خطوط/جمع
/// کل/فهرست اسناد این ترمیم) to build the full preview DTO.
/// </summary>
public sealed class GetPettyCashReplenishmentPreviewQueryHandler
    : IRequestHandler<GetPettyCashReplenishmentPreviewQuery, PettyCashReplenishmentPreviewDto?>
{
    private readonly IPettyCashFundReadRepository _fundReadRepository;
    private readonly IPettyCashReplenishmentReadRepository _replenishmentReadRepository;

    public GetPettyCashReplenishmentPreviewQueryHandler(
        IPettyCashFundReadRepository fundReadRepository,
        IPettyCashReplenishmentReadRepository replenishmentReadRepository)
    {
        _fundReadRepository = fundReadRepository;
        _replenishmentReadRepository = replenishmentReadRepository;
    }

    public async Task<PettyCashReplenishmentPreviewDto?> Handle(
        GetPettyCashReplenishmentPreviewQuery request, CancellationToken cancellationToken)
    {
        var fund = await _fundReadRepository.GetByIdAsync(request.FundId, request.VahedCode, cancellationToken);

        if (fund is null)
        {
            return null;
        }

        var linesResult = await _replenishmentReadRepository.GetUnlinkedApprovedGroupedAsync(
            request.FundId, request.VahedCode, cancellationToken);

        return new PettyCashReplenishmentPreviewDto(
            fund.Id,
            fund.Ceiling,
            fund.CashBalance,
            fund.ApprovedAmount,
            fund.ApprovedCount,
            fund.InFlightAmount,
            fund.InFlightCount,
            linesResult.Lines,
            linesResult.TotalAmount,
            fund.CashBalance + linesResult.TotalAmount,
            linesResult.DocIds);
    }
}
