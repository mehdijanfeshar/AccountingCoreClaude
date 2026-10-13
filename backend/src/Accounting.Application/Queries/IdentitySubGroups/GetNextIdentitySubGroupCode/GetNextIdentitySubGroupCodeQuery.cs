using System.Text.Json.Serialization;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Queries.IdentitySubGroups.GetNextIdentitySubGroupCode;

/// <summary>
/// کد پیشنهادی ویژگی (زیرگروه) بعدی در واحد و سال — بیشترین کد عددی + ۱، دو رقمی («01» اگر هیچ).
/// همان <c>GetMaxSubGroupCode</c> برنامهٔ مرجع؛ فقط پیشنهاد است و کاربر می‌تواند عوضش کند.
/// </summary>
public sealed record GetNextIdentitySubGroupCodeQuery(string Year) : IRequest<string>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class GetNextIdentitySubGroupCodeQueryHandler : IRequestHandler<GetNextIdentitySubGroupCodeQuery, string>
{
    private readonly IIdentitySubGroupReadRepository _readRepository;

    public GetNextIdentitySubGroupCodeQueryHandler(IIdentitySubGroupReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<string> Handle(GetNextIdentitySubGroupCodeQuery request, CancellationToken cancellationToken)
    {
        var codes = await _readRepository.GetCodesAsync(request.VahedCode, request.Year, cancellationToken);
        var max = codes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();
        return (max + 1).ToString("D2");
    }
}
