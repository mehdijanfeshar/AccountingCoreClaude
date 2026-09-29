using Accounting.Application.AccountCodes.Queries.GetTafsiliLevelItems;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Treasury.Queries.GetCustomerTafsilis;

public sealed class GetCustomerTafsilisQueryHandler
    : IRequestHandler<GetCustomerTafsilisQuery, PagedResult<TafsiliLookupItemDto>>
{
    private readonly ITreasurySettingReadRepository _settingReadRepository;
    private readonly ITreasuryBeneficiaryTafsiliReadRepository _tafsiliGroupReadRepository;

    public GetCustomerTafsilisQueryHandler(
        ITreasurySettingReadRepository settingReadRepository,
        ITreasuryBeneficiaryTafsiliReadRepository tafsiliGroupReadRepository)
    {
        _settingReadRepository = settingReadRepository;
        _tafsiliGroupReadRepository = tafsiliGroupReadRepository;
    }

    public async Task<PagedResult<TafsiliLookupItemDto>> Handle(
        GetCustomerTafsilisQuery request, CancellationToken cancellationToken)
    {
        var setting = await _settingReadRepository.GetByVahedAsync(request.VahedCode, cancellationToken);

        if (setting?.CustomerTafsilGroupId is not { } tafsilGroupId)
        {
            return new PagedResult<TafsiliLookupItemDto>
            {
                Items = Array.Empty<TafsiliLookupItemDto>(),
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = 0,
            };
        }

        return await _tafsiliGroupReadRepository.GetPagedAsync(
            tafsilGroupId, request.Search, request.PageNumber, request.PageSize, cancellationToken);
    }
}
