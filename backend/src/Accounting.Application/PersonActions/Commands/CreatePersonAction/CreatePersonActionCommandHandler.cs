using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.PersonActions.Commands.CreatePersonAction;

/// <summary>
/// Constructs the <see cref="TB_PERSON_ACTION"/> Domain entity from the command, stages it via
/// <see cref="IPersonActionRepository"/>, and owns the transaction boundary by calling
/// <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. <c>ADDUSERID</c> is sourced from
/// <see cref="ICurrentUser"/> (the authenticated caller) — never from the request. Note that
/// <c>ADDUSERID</c> (the auditing "who created this row" column) is distinct from
/// <see cref="Accounting.Application.PersonActions.Commands.CreatePersonAction.CreatePersonActionCommand.UserId"/>
/// (the subject of the grant window, i.e. "whose action window this is") — the two may differ.
/// </summary>
public sealed class CreatePersonActionCommandHandler : IRequestHandler<CreatePersonActionCommand, Guid>
{
    private readonly IPersonActionRepository _personActionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreatePersonActionCommandHandler(
        IPersonActionRepository personActionRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _personActionRepository = personActionRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreatePersonActionCommand request, CancellationToken cancellationToken)
    {
        var entity = new TB_PERSON_ACTION
        {
            ID = Guid.NewGuid(),
            USERNAME = request.UserName,
            USERID = request.UserId,
            FROMDATE = request.FromDate,
            TODATE = request.ToDate,
            STATUS = request.Status,
            OPERATORROLE = request.OperatorRole,
            VAHEDCODE = request.VahedCode,
            ADDUSERID = _currentUser.UserId,
            CREATEDDATE = DateTime.UtcNow,
            ISDELETED = false,
        };

        await _personActionRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.ID;
    }
}
