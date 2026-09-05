using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.Expenses.Commands.CreateExpense;

/// <summary>
/// Constructs the <see cref="TB_EXPENCE"/> Domain entity from the command, stages it via
/// <see cref="IExpenseRepository"/>, and owns the transaction boundary by calling
/// <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. <c>ADDUSERID</c> is sourced from
/// <see cref="ICurrentUser"/> (the authenticated caller) — never from the request.
///
/// <c>ID</c> is always generated application-side (<see cref="Guid.NewGuid"/>).
/// </summary>
public sealed class CreateExpenseCommandHandler : IRequestHandler<CreateExpenseCommand, Guid>
{
    private readonly IExpenseRepository _expenseRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateExpenseCommandHandler(
        IExpenseRepository expenseRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _expenseRepository = expenseRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateExpenseCommand request, CancellationToken cancellationToken)
    {
        var entity = new TB_EXPENCE
        {
            ID = Guid.NewGuid(),
            EXPENCECODE = request.ExpenseCode,
            EXPENCENAME = request.ExpenseName,
            DESCRIPTION = request.Description,
            DEFAULTAMOUNT = request.DefaultAmount,
            EXPENCEGROUP_ID = request.ExpenseGroupId,
            ACCOUNTCODE_ID = request.AccountCodeId,
            VAHEDCODE = request.VahedCode,
            ADDUSERID = _currentUser.UserId,
            CREATEDDATE = DateTime.UtcNow,
            ISDELETED = false,
        };

        await _expenseRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.ID;
    }
}
