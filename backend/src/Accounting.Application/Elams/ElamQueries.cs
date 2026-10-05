using Accounting.Application.Common;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using FluentValidation;
using MediatR;
using System.Text.Json.Serialization;

namespace Accounting.Application.Elams;

/// <summary>کارتابل اعلامیه‌های واحد جاری: صادره، رسیده یا درآمد.</summary>
public sealed record GetElamCartableQuery(
    string Year,
    ElamKind Kind,
    int PageNumber = 1,
    int PageSize = 20,
    string? SerialFrom = null,
    string? SerialTo = null,
    string? DateFrom = null,
    string? DateTo = null,
    string? DabirNo = null,
    string? CounterVahedCode = null) : IRequest<PagedResult<ElamCartableItemDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed record GetElamQuery(Guid Id) : IRequest<ElamViewDto>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed record GetElamUnitsQuery : IRequest<IReadOnlyList<ElamUnitDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class ElamQueryHandlers :
    IRequestHandler<GetElamCartableQuery, PagedResult<ElamCartableItemDto>>,
    IRequestHandler<GetElamQuery, ElamViewDto>,
    IRequestHandler<GetElamUnitsQuery, IReadOnlyList<ElamUnitDto>>
{
    private readonly IElamWorkflowReadRepository _readRepository;

    public ElamQueryHandlers(IElamWorkflowReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PagedResult<ElamCartableItemDto>> Handle(GetElamCartableQuery request, CancellationToken cancellationToken)
        => _readRepository.GetCartableAsync(
            new ElamCartableFilter(
                request.VahedCode, request.Year, request.Kind, request.PageNumber, request.PageSize,
                Blank(request.SerialFrom), Blank(request.SerialTo), Blank(request.DateFrom), Blank(request.DateTo),
                Blank(request.DabirNo), Blank(request.CounterVahedCode)),
            cancellationToken);

    public async Task<ElamViewDto> Handle(GetElamQuery request, CancellationToken cancellationToken)
        => await _readRepository.GetAsync(request.Id, request.VahedCode, cancellationToken)
            ?? throw new NotFoundException("Elam", request.Id);

    public Task<IReadOnlyList<ElamUnitDto>> Handle(GetElamUnitsQuery request, CancellationToken cancellationToken)
        => _readRepository.GetUnitsAsync(request.VahedCode, cancellationToken);

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}

public sealed class GetElamCartableQueryValidator : AbstractValidator<GetElamCartableQuery>
{
    public GetElamCartableQueryValidator()
    {
        RuleFor(x => x.Year).Matches("^[0-9]{4}$").WithMessage("سال مالی باید ۴ رقم باشد.");
        RuleFor(x => x.Kind).IsInEnum();
        RuleFor(x => x.PageNumber).InclusiveBetween(1, 100000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
        RuleFor(x => x.DateFrom).Matches(ElamRules.Date).When(x => !string.IsNullOrWhiteSpace(x.DateFrom));
        RuleFor(x => x.DateTo).Matches(ElamRules.Date).When(x => !string.IsNullOrWhiteSpace(x.DateTo));
        RuleFor(x => x.SerialFrom).MaximumLength(14);
        RuleFor(x => x.SerialTo).MaximumLength(14);
        RuleFor(x => x.DabirNo).MaximumLength(10);
        RuleFor(x => x.CounterVahedCode).MaximumLength(4);
    }
}

public sealed class GetElamQueryValidator : AbstractValidator<GetElamQuery>
{
    public GetElamQueryValidator() => RuleFor(x => x.Id).NotEmpty();
}
