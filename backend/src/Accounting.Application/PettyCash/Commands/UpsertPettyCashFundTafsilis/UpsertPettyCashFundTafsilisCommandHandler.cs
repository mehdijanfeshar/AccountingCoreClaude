using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.PettyCash.Commands.Common;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.UpsertPettyCashFundTafsilis;

/// <summary>
/// Reconciles <c>TB_PC_FUND_LINK_TAFSILI</c> against the requested replacement set, matching
/// existing rows on the (TafsiliId, LevelId) pair — same reconcile shape as
/// <c>UpdateExpenseCommandHandler.ReconcileTafsiliLinksAsync</c>. Surviving rows are left
/// untouched (not re-stamped).
/// </summary>
public sealed class UpsertPettyCashFundTafsilisCommandHandler : IRequestHandler<UpsertPettyCashFundTafsilisCommand>
{
    private readonly IPettyCashFundRepository _fundRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpsertPettyCashFundTafsilisCommandHandler(
        IPettyCashFundRepository fundRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _fundRepository = fundRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpsertPettyCashFundTafsilisCommand request, CancellationToken cancellationToken)
    {
        var fund = await _fundRepository.GetForUpdateAsync(request.FundId, request.VahedCode, cancellationToken)
            ?? throw new NotFoundException("PettyCashFund", request.FundId);

        var requested = request.Tafsilis ?? Array.Empty<PettyCashFundTafsiliLinkInput>();
        var existing = await _fundRepository.GetActiveFundTafsiliLinksAsync(fund.ID, cancellationToken);

        var requestedKeys = requested.Select(l => (l.TafsiliId, l.LevelId)).ToHashSet();

        foreach (var link in existing)
        {
            if (requestedKeys.Contains((link.TAFSILI_ID, link.LEVEL_ID)))
            {
                continue;
            }

            link.ISDELETED = true;
            link.CHANGEUSERID = _currentUser.UserId;
            link.UPDATEDDATE = DateTime.UtcNow;
        }

        var existingKeys = existing.Select(l => (l.TAFSILI_ID, l.LEVEL_ID)).ToHashSet();

        foreach (var link in requested)
        {
            if (existingKeys.Contains((link.TafsiliId, link.LevelId)))
            {
                continue;
            }

            await _fundRepository.AddFundTafsiliLinkAsync(
                new TB_PC_FUND_LINK_TAFSILI
                {
                    ID = Guid.NewGuid(),
                    FUND_ID = fund.ID,
                    TAFSILI_ID = link.TafsiliId,
                    LEVEL_ID = link.LevelId,
                    VAHEDCODE = request.VahedCode,
                    ADDUSERID = _currentUser.UserId,
                    CREATEDDATE = DateTime.UtcNow,
                    ISDELETED = false,
                },
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
