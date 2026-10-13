using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Queries.Vouchers.LineExtras;

/// <summary>
/// اطلاعات تکمیلی همهٔ ردیف‌های یک سند، آمادهٔ نمایش (عنوان‌ها به‌جای شناسه‌ها) — صفحهٔ «نمایش سند»:
/// تفصیلی‌ها، شناسهٔ حساب شناسه‌دار، ویژگی (مقادیر ثابت شناسنامه + متغیرها)، فیش/حواله و چک/چک صوری.
/// واحد از هدر (<see cref="IVahedScopedQuery"/>)؛ ردیف‌های واحد دیگر برنمی‌گردند.
/// </summary>
public sealed record GetVoucherLinesDisplayQuery(Guid VoucherHeadId)
    : IRequest<IReadOnlyList<VoucherLineDisplayDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed record VoucherLineDisplayDto(
    Guid DetailId,
    IReadOnlyList<VoucherLineTafsiliDisplay> Tafsilis,
    IReadOnlyList<VoucherLineLabeledValue> Attributes,
    IReadOnlyList<VoucherLineIdentityDisplay> Identities,
    VoucherLineReceiptDisplay? Receipt,
    VoucherLineChequeDisplay? Cheque);

public sealed record VoucherLineTafsiliDisplay(string? LevelName, string? Code, string? Name);

public sealed record VoucherLineLabeledValue(string Title, string? Value);

/// <param name="FixedValues">مقادیر ثابت شناسنامهٔ انتخاب‌شده.</param>
/// <param name="Values">مقادیر متغیر واردشده روی همین ردیف.</param>
public sealed record VoucherLineIdentityDisplay(
    string GroupTitle,
    IReadOnlyList<VoucherLineLabeledValue> FixedValues,
    IReadOnlyList<VoucherLineLabeledValue> Values);

/// <param name="Kind">۱ = فیش، ۲ = حواله.</param>
public sealed record VoucherLineReceiptDisplay(int Kind, string No, string Date);

public sealed record VoucherLineChequeDisplay(string No, string? Date, string? PayTo, bool IsSori, string? BookTitle);

public interface IVoucherLineDisplayReader
{
    Task<IReadOnlyList<VoucherLineDisplayDto>> GetAsync(Guid voucherHeadId, string vahedCode, CancellationToken ct);
}

public sealed class GetVoucherLinesDisplayHandler : IRequestHandler<GetVoucherLinesDisplayQuery, IReadOnlyList<VoucherLineDisplayDto>>
{
    private readonly IVoucherLineDisplayReader _reader;
    public GetVoucherLinesDisplayHandler(IVoucherLineDisplayReader reader) => _reader = reader;

    public Task<IReadOnlyList<VoucherLineDisplayDto>> Handle(GetVoucherLinesDisplayQuery request, CancellationToken ct)
        => _reader.GetAsync(request.VoucherHeadId, request.VahedCode, ct);
}
