using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.Accounts.Commands.CreateAccountCode;

/// <summary>
/// Constructs the <see cref="TB_ACCOUNTCODE"/> Domain entity from the command, stages it via
/// <see cref="IAccountCodeRepository"/>, and owns the transaction boundary by calling
/// <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once.
/// </summary>
public sealed class CreateAccountCodeCommandHandler : IRequestHandler<CreateAccountCodeCommand, Guid>
{
    private readonly IAccountCodeRepository _accountCodeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAccountCodeCommandHandler(IAccountCodeRepository accountCodeRepository, IUnitOfWork unitOfWork)
    {
        _accountCodeRepository = accountCodeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateAccountCodeCommand request, CancellationToken cancellationToken)
    {
        var entity = new TB_ACCOUNTCODE
        {
            ID = Guid.NewGuid(),
            TYPECODE = request.TypeCode,
            PARENTID = request.ParentId,
            ACCCODE = request.AccCode,
            ACCCODENAME = request.AccCodeName,
            TYPEACTIVITY = request.TypeActivity,
            SOURCEANDCONSUME_ID = request.SourceAndConsumeId,
            IDENTYGROUPS_ID = request.IdentyGroupsId,
            TYPEACCCODE = request.TypeAccCode,
            ADDUSERID = request.AddUserId,
            MOINFORCLOSE = request.MoInforClose,
            TYPEACTION = request.TypeAction,
            CREATEDDATE = DateTime.UtcNow,
            ISDELETED = false,
        };

        await _accountCodeRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.ID;
    }
}
