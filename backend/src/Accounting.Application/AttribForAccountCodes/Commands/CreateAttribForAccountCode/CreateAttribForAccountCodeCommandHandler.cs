using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.AttribForAccountCodes.Commands.CreateAttribForAccountCode;

/// <summary>
/// Constructs the <see cref="TB_ATTRIBFORACCOUNTCODE"/> Domain entity from the command, stages it
/// via <see cref="IAttribForAccountCodeRepository"/>, and owns the transaction boundary by calling
/// <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. <c>ADDUSERID</c> is sourced from
/// <see cref="ICurrentUser"/> (the authenticated caller) — never from the request — so it cannot
/// be forged by the client.
///
/// <c>ID</c> is always generated application-side (<see cref="Guid.NewGuid"/>); the Oracle
/// <c>ID</c> column has no <c>sys_guid()</c> default on this table.
/// </summary>
public sealed class CreateAttribForAccountCodeCommandHandler : IRequestHandler<CreateAttribForAccountCodeCommand, Guid>
{
    private readonly IAttribForAccountCodeRepository _attribForAccountCodeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateAttribForAccountCodeCommandHandler(
        IAttribForAccountCodeRepository attribForAccountCodeRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _attribForAccountCodeRepository = attribForAccountCodeRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateAttribForAccountCodeCommand request, CancellationToken cancellationToken)
    {
        var entity = new TB_ATTRIBFORACCOUNTCODE
        {
            ID = Guid.NewGuid(),
            ACCOUNTCODE_ID = request.AccountCodeId,
            ATTRIBBOXNO = request.AttribBoxNo,
            FLAG = request.Flag,
            LENATR = request.LenAtr,
            ATTRIBSUM = request.AttribSum,
            CONTROLID = request.ControlId,
            VAHEDCODE = request.VahedCode,
            YEAR = request.Year,
            ADDUSERID = _currentUser.UserId,
            CREATEDDATE = DateTime.UtcNow,
            ISDELETED = false,
        };

        await _attribForAccountCodeRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.ID;
    }
}
