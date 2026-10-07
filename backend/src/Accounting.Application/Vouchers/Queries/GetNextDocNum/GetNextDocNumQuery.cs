using System.Text.Json.Serialization;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using FluentValidation;
using MediatR;

namespace Accounting.Application.Vouchers.Queries.GetNextDocNum;

/// <summary>
/// شمارهٔ پیشنهادی سند بعدی واحد هدر در سال = بزرگ‌ترین شمارهٔ عددی + ۱ (۶ رقم). فقط پیش‌فرض فرم است؛
/// کاربر می‌تواند عوضش کند و یکتایی را همان <c>UK_VOUCHERHEAD_NUMBER</c> (۴۰۹) تضمین می‌کند.
/// </summary>
public sealed record GetNextDocNumQuery(string Year) : IRequest<NextDocNumDto>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed record NextDocNumDto(string DocNum);

public sealed class GetNextDocNumQueryValidator : AbstractValidator<GetNextDocNumQuery>
{
    public GetNextDocNumQueryValidator()
    {
        RuleFor(x => x.Year).NotEmpty().Matches("^[0-9]{4}$").WithMessage("سال مالی باید چهار رقم باشد.");
    }
}

public sealed class GetNextDocNumQueryHandler : IRequestHandler<GetNextDocNumQuery, NextDocNumDto>
{
    private readonly IVoucherHeadRepository _voucherHeadRepository;

    public GetNextDocNumQueryHandler(IVoucherHeadRepository voucherHeadRepository)
    {
        _voucherHeadRepository = voucherHeadRepository;
    }

    public async Task<NextDocNumDto> Handle(GetNextDocNumQuery request, CancellationToken cancellationToken)
        => new(await _voucherHeadRepository.GetNextDocNumAsync(request.VahedCode, request.Year, cancellationToken));
}
