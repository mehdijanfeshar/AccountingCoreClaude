using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.WhiteLists.Commands.CreateWhiteList;

/// <summary>
/// Constructs the <see cref="TB_WHITELIST"/> Domain entity from the command, stages it via
/// <see cref="IWhiteListRepository"/>, and owns the transaction boundary by calling
/// <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. <c>ADDUSERID</c> is sourced from
/// <see cref="ICurrentUser"/> (the authenticated caller) — never from the request — so it
/// cannot be forged by the client.
///
/// <c>ID</c> is always generated application-side (<see cref="Guid.NewGuid"/>); the Oracle
/// <c>ID</c> column has a <c>sys_guid()</c> default, but that default is therefore never
/// exercised — avoiding the recorded 🔴 CLAUDE.md risk where <c>sys_guid()</c> yields a
/// non-dashed value that the project's strict <c>GuidToChar36Converter</c> would reject on read.
/// </summary>
public sealed class CreateWhiteListCommandHandler : IRequestHandler<CreateWhiteListCommand, Guid>
{
    private readonly IWhiteListRepository _whiteListRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateWhiteListCommandHandler(
        IWhiteListRepository whiteListRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _whiteListRepository = whiteListRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateWhiteListCommand request, CancellationToken cancellationToken)
    {
        var entity = new TB_WHITELIST
        {
            ID = Guid.NewGuid(),
            ACCOUNTCODE_ID = request.AccountCodeId,
            VAHEDTYPE_ID = request.VahedTypeId,
            VAHEDINFO_ID = request.VahedInfoId,
            ADDUSERID = _currentUser.UserId,
            CREATEDDATE = DateTime.UtcNow,
            ISDELETED = false,
            FROMAUTHORIZEDDATE = request.FromAuthorizedDate,
            TOAUTHORIZEDDATE = request.ToAuthorizedDate,
            FROMLIMITATIONDATE = request.FromLimitationDate,
            TOLIMITATIONDATE = request.ToLimitationDate,
        };

        await _whiteListRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.ID;
    }
}
