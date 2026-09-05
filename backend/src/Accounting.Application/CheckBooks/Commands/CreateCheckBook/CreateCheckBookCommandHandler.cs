using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.CheckBooks.Commands.CreateCheckBook;

/// <summary>
/// Constructs the <see cref="TB_CHECKBOOK"/> Domain entity from the command, stages it via
/// <see cref="ICheckBookRepository"/>, and owns the transaction boundary by calling
/// <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. <c>ADDUSERID</c> is sourced from
/// <see cref="ICurrentUser"/> (the authenticated caller) — never from the request — so it cannot
/// be forged by the client.
///
/// <c>ID</c> is always generated application-side (<see cref="Guid.NewGuid"/>); the Oracle
/// column has no <c>sys_guid()</c> default, so this simply follows the project-wide convention.
/// </summary>
public sealed class CreateCheckBookCommandHandler : IRequestHandler<CreateCheckBookCommand, Guid>
{
    private readonly ICheckBookRepository _checkBookRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateCheckBookCommandHandler(
        ICheckBookRepository checkBookRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _checkBookRepository = checkBookRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateCheckBookCommand request, CancellationToken cancellationToken)
    {
        var entity = new TB_CHECKBOOK
        {
            ID = Guid.NewGuid(),
            ACCOUNT_ID = request.AccountId,
            CHECKBOOK_TITLE = request.CheckBookTitle,
            CHECKBOOK_DATE = request.CheckBookDate,
            FROMCHECKNUMBER = request.FromCheckNumber,
            TOCHECKNUMBER = request.ToCheckNumber,
            CHECKTYPE_ID = request.CheckTypeId,
            VAHEDCODE = request.VahedCode,
            CHECKBOOK_TYPE = request.CheckBookType,
            SERIAL = request.Serial,
            ADDUSERID = _currentUser.UserId,
            CREATEDDATE = DateTime.UtcNow,
            ISDELETED = false,
        };

        await _checkBookRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.ID;
    }
}
