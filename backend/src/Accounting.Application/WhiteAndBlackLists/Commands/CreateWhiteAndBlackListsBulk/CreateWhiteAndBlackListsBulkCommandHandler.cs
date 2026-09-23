using Accounting.Application.Common.Interfaces;
using Accounting.Application.WhiteAndBlackLists.Queries;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.WhiteAndBlackLists.Commands.CreateWhiteAndBlackListsBulk;

/// <summary>
/// Expands the command's two id lists into their cartesian product, drops the combinations that
/// already exist, stages the rest, and saves once — so the whole grant is a single transaction.
///
/// <c>ADDUSERID</c> comes from <see cref="ICurrentUser"/> and <c>ID</c> is generated
/// application-side, for the same two reasons spelled out on
/// <see cref="CreateWhiteAndBlackList.CreateWhiteAndBlackListCommandHandler"/>: it cannot be
/// forged by the client, and the Oracle <c>sys_guid()</c> column default would produce a
/// non-dashed value that <c>GuidToChar36Converter</c> rejects on read.
/// </summary>
public sealed class CreateWhiteAndBlackListsBulkCommandHandler
    : IRequestHandler<CreateWhiteAndBlackListsBulkCommand, CreateWhiteAndBlackListsBulkResult>
{
    private readonly IWhiteAndBlackListRepository _whiteAndBlackListRepository;
    private readonly IWhiteAndBlackListReadRepository _readRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateWhiteAndBlackListsBulkCommandHandler(
        IWhiteAndBlackListRepository whiteAndBlackListRepository,
        IWhiteAndBlackListReadRepository readRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _whiteAndBlackListRepository = whiteAndBlackListRepository;
        _readRepository = readRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<CreateWhiteAndBlackListsBulkResult> Handle(
        CreateWhiteAndBlackListsBulkCommand request,
        CancellationToken cancellationToken)
    {
        var accountCodeIds = request.AccountCodeIds.Distinct().ToArray();
        var vahedTypeIds = request.VahedTypeIds.Distinct().ToArray();

        // See the command's XML doc: State decides which of the two date pairs carries the range.
        var isAllowed = request.State == WhiteBlackListState.Allowed;
        var fromAuthorized = isAllowed ? request.FromDate : null;
        var toAuthorized = isAllowed ? request.ToDate : null;
        var fromLimitation = isAllowed ? null : request.FromDate;
        var toLimitation = isAllowed ? null : request.ToDate;

        var existing = (await _readRepository.GetExistingKeysAsync(accountCodeIds, cancellationToken))
            .ToHashSet();

        // Also tracks the keys staged by THIS request, not just the ones already in the database.
        // Both guards are needed: the database one stops a re-grant, and this one stops a single
        // request from staging the same key twice, which SaveChanges would surface as an Oracle
        // UK_WHITEANDBLACKLIST violation that rolls the entire grant back.
        var staged = new HashSet<WhiteAndBlackListKey>();
        var createdIds = new List<Guid>();
        var skipped = 0;
        var now = DateTime.UtcNow;

        foreach (var accountCodeId in accountCodeIds)
        {
            foreach (var vahedTypeId in vahedTypeIds)
            {
                var key = new WhiteAndBlackListKey(accountCodeId, vahedTypeId, fromAuthorized, toAuthorized);

                if (existing.Contains(key) || !staged.Add(key))
                {
                    skipped++;
                    continue;
                }

                var entity = new TB_WHITEANDBLACKLIST
                {
                    ID = Guid.NewGuid(),
                    ACCOUNTCODE_ID = accountCodeId,
                    VAHEDTYPE_ID = vahedTypeId,
                    ADDUSERID = _currentUser.UserId,
                    CREATEDDATE = now,
                    ISDELETED = false,
                    FROMAUTHORIZEDDATE = fromAuthorized,
                    TOAUTHORIZEDDATE = toAuthorized,
                    FROMLIMITATIONDATE = fromLimitation,
                    TOLIMITATIONDATE = toLimitation,
                    STATE = request.State,
                };

                await _whiteAndBlackListRepository.AddAsync(entity, cancellationToken);
                createdIds.Add(entity.ID);
            }
        }

        // Saved unconditionally, including when nothing was staged: an empty SaveChanges is a
        // no-op, and special-casing it would add a branch with no observable difference.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateWhiteAndBlackListsBulkResult(createdIds.Count, skipped, createdIds);
    }
}
