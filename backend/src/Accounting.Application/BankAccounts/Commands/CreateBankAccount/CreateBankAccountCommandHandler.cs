using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.BankAccounts.Commands.CreateBankAccount;

/// <summary>
/// Constructs the <see cref="TB_ACCOUNT"/> Domain entity from the command, stages it via
/// <see cref="IBankAccountRepository"/>, and owns the transaction boundary by calling
/// <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. <c>ADDUSERID</c> is sourced from
/// <see cref="ICurrentUser"/> (the authenticated caller) — never from the request — so it cannot
/// be forged by the client, even though the column itself is nullable in Legacy.
///
/// <c>request.VahedCode</c> is equally unforgeable, just enforced one layer earlier: by the time
/// this handler runs, <c>VahedScopeBehavior</c> has already overwritten it with
/// <see cref="ICurrentUser.VahedCode"/>, so this handler can trust the field at face value and
/// simply map it onto <c>TB_ACCOUNT.VAHEDCODE</c> — it does not read <see cref="ICurrentUser"/>
/// directly for this field the way it does for <c>ADDUSERID</c>.
///
/// <c>ID</c> is always generated application-side (<see cref="Guid.NewGuid"/>) — this table has
/// no <c>sys_guid()</c> default in <c>LegacyDbContext</c>, but generating it here regardless
/// keeps this handler symmetric with every other Create handler in the project and never relies
/// on an Oracle DEFAULT.
/// </summary>
public sealed class CreateBankAccountCommandHandler : IRequestHandler<CreateBankAccountCommand, Guid>
{
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateBankAccountCommandHandler(
        IBankAccountRepository bankAccountRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _bankAccountRepository = bankAccountRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateBankAccountCommand request, CancellationToken cancellationToken)
    {
        var entity = new TB_ACCOUNT
        {
            ID = Guid.NewGuid(),
            ACCOUNTNUMBER = request.AccountNumber,
            ACCOUNTHOLDER = request.AccountHolder,
            CARDNUMBER = request.CardNumber,
            SHEBANUMBER = request.ShebaNumber,
            FIRSTAMOUNT = request.FirstAmount,
            BANK_ID = request.BankId,
            BRANCH_ID = request.BranchId,
            ACCOUNTTYPE_ID = request.AccountTypeId,
            ACCOUNTCODE_ID = request.AccountCodeId,
            CHECKFILE = request.CheckFile,
            VAHEDCODE = request.VahedCode,
            ACCOUNTOPENINGDATE = request.AccountOpeningDate,
            ADDUSERID = _currentUser.UserId,
            CREATEDDATE = DateTime.UtcNow,
            ISDELETED = false,
        };

        await _bankAccountRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.ID;
    }
}
