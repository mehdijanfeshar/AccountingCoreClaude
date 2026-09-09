using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.RevolvingFunds.Commands.CreateRevolvingFund;

/// <summary>
/// Constructs the <see cref="TB_REVOLVING_FUND"/> Domain entity from the command, stages it via
/// <see cref="IRevolvingFundRepository"/>, and owns the transaction boundary by calling
/// <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. <c>ADDUSERID</c> is sourced from
/// <see cref="ICurrentUser"/> (the authenticated caller) — never from the request — so it
/// cannot be forged by the client.
///
/// <c>ID</c> is always generated application-side (<see cref="Guid.NewGuid"/>) — this table has
/// no <c>sys_guid()</c> default in <c>LegacyDbContext</c>, but generating it here regardless
/// keeps this handler symmetric with every other Create handler in the project and never relies
/// on an Oracle DEFAULT.
///
/// <c>request.VahedCode</c> is equally unforgeable, just enforced one layer earlier: by the
/// time this handler runs, <c>VahedScopeBehavior</c> has already overwritten it with
/// <see cref="ICurrentUser.VahedCode"/>, so this handler can trust the field at face value and
/// simply map it onto <c>TB_REVOLVING_FUND.VAHEDCODE</c> — it does not read
/// <see cref="ICurrentUser"/> directly for this field the way it does for <c>ADDUSERID</c>.
/// </summary>
public sealed class CreateRevolvingFundCommandHandler : IRequestHandler<CreateRevolvingFundCommand, Guid>
{
    private readonly IRevolvingFundRepository _revolvingFundRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateRevolvingFundCommandHandler(
        IRevolvingFundRepository revolvingFundRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _revolvingFundRepository = revolvingFundRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateRevolvingFundCommand request, CancellationToken cancellationToken)
    {
        var entity = new TB_REVOLVING_FUND
        {
            ID = Guid.NewGuid(),
            CODE = request.Code,
            NAME = request.Name,
            DESCRIPTION = request.Description,
            DEFAULTAMOUNT = request.DefaultAmount,
            ACCOUNTCODE_ID = request.AccountCodeId,
            VAHEDCODE = request.VahedCode,
            YEAR = request.Year,
            ADDUSERID = _currentUser.UserId,
            CREATEDDATE = DateTime.UtcNow,
            ISDELETED = false,
        };

        await _revolvingFundRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.ID;
    }
}
