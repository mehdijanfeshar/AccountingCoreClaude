using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Treasury.Queries.GetTreasurySetting;

public sealed class GetTreasurySettingQueryHandler : IRequestHandler<GetTreasurySettingQuery, TreasurySettingDto?>
{
    private readonly ITreasurySettingReadRepository _settingReadRepository;

    public GetTreasurySettingQueryHandler(ITreasurySettingReadRepository settingReadRepository)
    {
        _settingReadRepository = settingReadRepository;
    }

    public Task<TreasurySettingDto?> Handle(GetTreasurySettingQuery request, CancellationToken cancellationToken)
        => _settingReadRepository.GetByVahedAsync(request.VahedCode, cancellationToken);
}
