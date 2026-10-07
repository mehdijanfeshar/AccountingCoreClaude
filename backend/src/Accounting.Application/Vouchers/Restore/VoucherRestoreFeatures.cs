using System.Text.Json.Serialization;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace Accounting.Application.Vouchers.Restore;

// بازگردانی سند حذف‌شده (فاز ۵۲). حذف سند نرم است؛ این‌جا واحد سندهای حذف‌شدهٔ سالش را می‌بیند و برمی‌گرداند.

public sealed record DeletedVoucherDto(
    Guid Id,
    string? DocNum,
    string? DateDoc,
    string? HeadDesc,
    DateTime? DeletedAt,
    string? DeletedBy,
    int LineCount,
    decimal Debtor,
    decimal Creditor);

public sealed record GetDeletedVouchersQuery(string Year) : IRequest<IReadOnlyList<DeletedVoucherDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class GetDeletedVouchersQueryValidator : AbstractValidator<GetDeletedVouchersQuery>
{
    public GetDeletedVouchersQueryValidator() => RuleFor(x => x.Year).NotEmpty().Matches("^[0-9]{4}$");
}

public sealed class GetDeletedVouchersQueryHandler : IRequestHandler<GetDeletedVouchersQuery, IReadOnlyList<DeletedVoucherDto>>
{
    private readonly IVoucherRestoreRepository _repo;

    public GetDeletedVouchersQueryHandler(IVoucherRestoreRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<DeletedVoucherDto>> Handle(GetDeletedVouchersQuery q, CancellationToken ct)
        => (await _repo.ListDeletedAsync(q.VahedCode, q.Year, ct))
            .Select(r => new DeletedVoucherDto(r.Id, r.DocNum, r.DateDoc, r.HeadDesc, r.DeletedAt, r.DeletedBy, r.LineCount, r.Debtor, r.Creditor))
            .ToList();
}

public sealed record RestoreVoucherCommand(Guid Id) : IRequest<RestoreVoucherResult>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <param name="DocNum">شمارهٔ سند پس از بازگردانی.</param>
/// <param name="Renumbered">شمارهٔ قبلی در این فاصله به سند دیگری داده شده بود و شمارهٔ تازه گرفت.</param>
public sealed record RestoreVoucherResult(Guid Id, string DocNum, bool Renumbered, int RestoredLines);

public sealed class RestoreVoucherCommandValidator : AbstractValidator<RestoreVoucherCommand>
{
    public RestoreVoucherCommandValidator() => RuleFor(x => x.Id).NotEqual(Guid.Empty);
}

/// <summary>
/// سرسند و ردیف‌ها/لینک‌هایی که هم‌زمان با آن حذف شدند برمی‌گردند؛ سند در وضعیت «یادداشت» برمی‌گردد
/// (کاربر دوباره کنترل و موقت می‌کند). اگر شماره‌اش گرفته شده باشد، شمارهٔ بعدی واحد را می‌گیرد.
/// </summary>
public sealed class RestoreVoucherCommandHandler : IRequestHandler<RestoreVoucherCommand, RestoreVoucherResult>
{
    private readonly IVoucherRestoreRepository _repo;
    private readonly IVoucherHeadRepository _headRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public RestoreVoucherCommandHandler(
        IVoucherRestoreRepository repo,
        IVoucherHeadRepository headRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _repo = repo;
        _headRepository = headRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<RestoreVoucherResult> Handle(RestoreVoucherCommand c, CancellationToken ct)
    {
        var head = await _repo.GetDeletedForRestoreAsync(c.Id, c.VahedCode, ct)
            ?? throw new NotFoundException(nameof(TB_VOUCHERSHEAD), c.Id);

        var year = head.YEAR ?? throw new BusinessRuleException("سال مالی این سند مشخص نیست و قابل بازگردانی نیست.");
        var renumbered = false;
        if (string.IsNullOrWhiteSpace(head.DOC_NUM)
            || await _repo.IsDocNumTakenAsync(c.VahedCode, year, head.DOC_NUM, head.ID, ct))
        {
            head.DOC_NUM = await _headRepository.GetNextDocNumAsync(c.VahedCode, year, ct);
            renumbered = true;
        }

        var now = DateTime.UtcNow;
        var lines = await _repo.RestoreTreeAsync(head.ID, head.UPDATEDDATE, _currentUser.UserId, now, ct);

        head.ISDELETED = false;
        head.DOCLIFE = DocLife.Draft;
        head.CHANGEUSERID = _currentUser.UserId;
        head.UPDATEDDATE = now;

        await _unitOfWork.SaveChangesAsync(ct);
        return new RestoreVoucherResult(head.ID, head.DOC_NUM!, renumbered, lines);
    }
}
