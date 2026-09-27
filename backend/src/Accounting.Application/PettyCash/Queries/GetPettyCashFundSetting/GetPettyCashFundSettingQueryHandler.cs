using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashFundSetting;

public sealed class GetPettyCashFundSettingQueryHandler
    : IRequestHandler<GetPettyCashFundSettingQuery, PettyCashFundSettingDto>
{
    private readonly IRevolvingFundReadRepository _revolvingFundReadRepository;
    private readonly IPettyCashFundSettingRepository _fundSettingRepository;

    public GetPettyCashFundSettingQueryHandler(
        IRevolvingFundReadRepository revolvingFundReadRepository,
        IPettyCashFundSettingRepository fundSettingRepository)
    {
        _revolvingFundReadRepository = revolvingFundReadRepository;
        _fundSettingRepository = fundSettingRepository;
    }

    public async Task<PettyCashFundSettingDto> Handle(GetPettyCashFundSettingQuery request, CancellationToken cancellationToken)
    {
        // Ownership/existence of the fund itself. Throws UnitAccessDeniedException (403) for a
        // cross-unit fund; returns null (→ 404 below) for a fund that simply does not exist.
        var fund = await _revolvingFundReadRepository.GetByIdAsync(request.FundId, request.VahedCode, cancellationToken);

        if (fund is null || fund.IsDeleted == true)
        {
            throw new NotFoundException("RevolvingFund", request.FundId);
        }

        var setting = await _fundSettingRepository.GetByFundIdAsync(request.FundId, cancellationToken);

        // Frontend contract: "no settings configured yet" is reported as 404, exactly like a
        // missing fund — see this query's XML doc.
        if (setting is null || setting.ISDELETED)
        {
            throw new NotFoundException("PettyCashFundSetting", request.FundId);
        }

        return new PettyCashFundSettingDto(
            setting.CUSTODIAN_USERID,
            setting.CUSTODIAN_NAME,
            setting.PER_DOC_LIMIT,
            setting.ALERT_THRESHOLD_PERCENT,
            setting.SETTLEMENT_PERIOD);
    }
}
