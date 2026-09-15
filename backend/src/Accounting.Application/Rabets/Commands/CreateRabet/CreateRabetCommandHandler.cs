using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.Rabets.Commands.CreateRabet;

/// <summary>
/// Constructs the <see cref="TB_RABET"/> Domain entity from the command, stages it via
/// <see cref="IRabetRepository"/>, and owns the transaction boundary by calling
/// <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. <c>ADDUSERID</c> is sourced from
/// <see cref="ICurrentUser"/> (the authenticated caller) — never from the request — so it
/// cannot be forged by the client, even though the column itself is nullable in Legacy.
///
/// <c>ID</c> is always generated application-side (<see cref="Guid.NewGuid"/>); the Oracle
/// <c>ID</c> column has a <c>sys_guid()</c> default, but that default is therefore never
/// exercised — avoiding the recorded 🔴 CLAUDE.md risk where <c>sys_guid()</c> yields a
/// non-dashed value that the project's strict <c>GuidToChar36Converter</c> would reject on read.
/// </summary>
public sealed class CreateRabetCommandHandler : IRequestHandler<CreateRabetCommand, Guid>
{
    private readonly IRabetRepository _rabetRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateRabetCommandHandler(
        IRabetRepository rabetRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _rabetRepository = rabetRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateRabetCommand request, CancellationToken cancellationToken)
    {
        var entity = new TB_RABET
        {
            ID = Guid.NewGuid(),
            RABETTYPE_ID = request.RabetTypeId,
            ACCOUNTCODE_ID = request.AccountCodeId,
            ADDUSERID = _currentUser.UserId,
            CREATEDDATE = DateTime.UtcNow,
            ISDELETED = false,
        };

        await _rabetRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.ID;
    }
}
