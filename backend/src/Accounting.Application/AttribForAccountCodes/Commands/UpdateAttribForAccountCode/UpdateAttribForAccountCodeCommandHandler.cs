using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.AttribForAccountCodes.Commands.UpdateAttribForAccountCode;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_ATTRIBFORACCOUNTCODE"/> row via
/// <see cref="IAttribForAccountCodeRepository.GetForUpdateAsync"/> (change-tracked), overwrites
/// every writable field from the command, stamps audit columns, and owns the transaction boundary
/// by calling <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist or is already soft-deleted (<c>ISDELETED</c> is non-nullable <c>bool</c> on
/// this table, so a plain <see langword="true"/> check is sufficient). <c>CHANGEUSERID</c> is
/// sourced from <see cref="ICurrentUser"/> (the authenticated caller) — never from the request.
/// </summary>
public sealed class UpdateAttribForAccountCodeCommandHandler : IRequestHandler<UpdateAttribForAccountCodeCommand>
{
    private readonly IAttribForAccountCodeRepository _attribForAccountCodeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateAttribForAccountCodeCommandHandler(
        IAttribForAccountCodeRepository attribForAccountCodeRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _attribForAccountCodeRepository = attribForAccountCodeRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateAttribForAccountCodeCommand request, CancellationToken cancellationToken)
    {
        var entity = await _attribForAccountCodeRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null || entity.ISDELETED)
        {
            throw new NotFoundException("AttribForAccountCode", request.Id);
        }

        entity.ACCOUNTCODE_ID = request.AccountCodeId;
        entity.ATTRIBBOXNO = request.AttribBoxNo;
        entity.FLAG = request.Flag;
        entity.LENATR = request.LenAtr;
        entity.ATTRIBSUM = request.AttribSum;
        entity.CONTROLID = request.ControlId;
        entity.VAHEDCODE = request.VahedCode;
        entity.YEAR = request.Year;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
