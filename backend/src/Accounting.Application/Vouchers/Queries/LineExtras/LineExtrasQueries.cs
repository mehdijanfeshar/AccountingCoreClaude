using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Application.Vouchers.Commands.Common;
using MediatR;

namespace Accounting.Application.Vouchers.Queries.LineExtras;

/// <summary>
/// ردیف با این معین و این تفصیلی‌ها چه اطلاعات تکمیلی‌ای لازم دارد: شناسه‌ها (با نوع/طول)، ویژگی‌ها
/// (فیلدهای ثابت و متغیر) و بانکی بودن (چک / فیش).
/// </summary>
public sealed record GetVoucherLineRequirementsQuery(Guid AccountId, IReadOnlyList<Guid> TafsiliIds, string Year)
    : IRequest<VoucherLineRequirements>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class GetVoucherLineRequirementsHandler : IRequestHandler<GetVoucherLineRequirementsQuery, VoucherLineRequirements>
{
    private readonly IVoucherLineExtrasStore _store;
    public GetVoucherLineRequirementsHandler(IVoucherLineExtrasStore store) => _store = store;

    public Task<VoucherLineRequirements> Handle(GetVoucherLineRequirementsQuery request, CancellationToken ct)
        => _store.GetRequirementsAsync(request.AccountId, request.TafsiliIds, request.VahedCode, request.Year, ct);
}

/// <summary>شناسنامه‌های یک ویژگی در واحد و سال، با مقدار فیلدهای ثابت هرکدام.</summary>
public sealed record ListIdentityHeadOptionsQuery(Guid GroupId, string Year)
    : IRequest<IReadOnlyList<IdentityHeadOption>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class ListIdentityHeadOptionsHandler : IRequestHandler<ListIdentityHeadOptionsQuery, IReadOnlyList<IdentityHeadOption>>
{
    private readonly IVoucherLineExtrasStore _store;
    public ListIdentityHeadOptionsHandler(IVoucherLineExtrasStore store) => _store = store;

    public Task<IReadOnlyList<IdentityHeadOption>> Handle(ListIdentityHeadOptionsQuery request, CancellationToken ct)
        => _store.ListHeadsAsync(request.GroupId, request.VahedCode, request.Year, ct);
}

/// <summary>شناسه/ویژگی/فیش ذخیره‌شدهٔ یک ردیف (ویرایش سند).</summary>
public sealed record GetVoucherLineExtrasQuery(Guid DetailId) : IRequest<VoucherLineExtrasDto>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class GetVoucherLineExtrasHandler : IRequestHandler<GetVoucherLineExtrasQuery, VoucherLineExtrasDto>
{
    private readonly IVoucherLineExtrasStore _store;
    public GetVoucherLineExtrasHandler(IVoucherLineExtrasStore store) => _store = store;

    public Task<VoucherLineExtrasDto> Handle(GetVoucherLineExtrasQuery request, CancellationToken ct)
        => _store.GetSavedAsync(request.DetailId, request.VahedCode, ct);
}
