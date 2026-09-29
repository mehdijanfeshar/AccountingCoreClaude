using Accounting.Application.AccountCodes.Queries.GetTafsiliLevelItems;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Treasury.Queries.GetBeneficiaryTafsilis;

public sealed class GetBeneficiaryTafsilisQueryHandler
    : IRequestHandler<GetBeneficiaryTafsilisQuery, PagedResult<TafsiliLookupItemDto>>
{
    private readonly ITreasurySettingReadRepository _settingReadRepository;
    private readonly ITreasuryBeneficiaryTafsiliReadRepository _beneficiaryTafsiliReadRepository;

    public GetBeneficiaryTafsilisQueryHandler(
        ITreasurySettingReadRepository settingReadRepository,
        ITreasuryBeneficiaryTafsiliReadRepository beneficiaryTafsiliReadRepository)
    {
        _settingReadRepository = settingReadRepository;
        _beneficiaryTafsiliReadRepository = beneficiaryTafsiliReadRepository;
    }

    public async Task<PagedResult<TafsiliLookupItemDto>> Handle(
        GetBeneficiaryTafsilisQuery request, CancellationToken cancellationToken)
    {
        var setting = await _settingReadRepository.GetByVahedAsync(request.VahedCode, cancellationToken);

        if (setting?.BeneficiaryTafsilGroupId is not { } tafsilGroupId)
        {
            return new PagedResult<TafsiliLookupItemDto>
            {
                Items = Array.Empty<TafsiliLookupItemDto>(),
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = 0,
            };
        }

        return await _beneficiaryTafsiliReadRepository.GetPagedAsync(
            tafsilGroupId, request.Search, request.PageNumber, request.PageSize, cancellationToken);
    }
}
