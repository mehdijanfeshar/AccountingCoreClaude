using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.AccountExceptions.Commands.CreateAccountException;

/// <summary>
/// Constructs the <see cref="TB_ACCOUNTEXCEPTION"/> Domain entity from the command, stages it
/// via <see cref="IAccountExceptionRepository"/>, and owns the transaction boundary by calling
/// <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. <c>ADDUSERID</c> is sourced from
/// <see cref="ICurrentUser"/> (the authenticated caller) — never from the request.
/// </summary>
public sealed class CreateAccountExceptionCommandHandler : IRequestHandler<CreateAccountExceptionCommand, Guid>
{
    private readonly IAccountExceptionRepository _accountExceptionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateAccountExceptionCommandHandler(
        IAccountExceptionRepository accountExceptionRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _accountExceptionRepository = accountExceptionRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateAccountExceptionCommand request, CancellationToken cancellationToken)
    {
        var entity = new TB_ACCOUNTEXCEPTION
        {
            ID = Guid.NewGuid(),
            ACCOUNTCOE_ID = request.AccountCoeId,
            VAHEDTYPE_ID = request.VahedTypeId,
            ADDUSERID = _currentUser.UserId,
            CREATEDDATE = DateTime.UtcNow,
            ISDELETED = false,
        };

        await _accountExceptionRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.ID;
    }
}
