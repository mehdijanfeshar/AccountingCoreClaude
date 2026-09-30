using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Treasury.Commands.Common;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Commands.UpdateBankStatement;

public sealed class UpdateBankStatementCommandHandler : IRequestHandler<UpdateBankStatementCommand>
{
    private static readonly TreasuryRole[] EditorRoles = { TreasuryRole.Treasurer, TreasuryRole.SeniorAccountant };

    private readonly ITreasuryBankStatementRepository _statementRepository;
    private readonly IBankAccountReadRepository _bankAccountReadRepository;
    private readonly ITreasuryRoleAuthorizer _roleAuthorizer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateBankStatementCommandHandler(
        ITreasuryBankStatementRepository statementRepository,
        IBankAccountReadRepository bankAccountReadRepository,
        ITreasuryRoleAuthorizer roleAuthorizer,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _statementRepository = statementRepository;
        _bankAccountReadRepository = bankAccountReadRepository;
        _roleAuthorizer = roleAuthorizer;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateBankStatementCommand request, CancellationToken cancellationToken)
    {
        await _roleAuthorizer.EnsureHasRoleAsync(request.VahedCode, EditorRoles, cancellationToken);

        var statement = await _statementRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken);

        if (statement is null || statement.ISDELETED)
        {
            throw new NotFoundException("BankStatement", request.Id);
        }

        BankStatementEditability.EnsureOpen(statement.ID, statement.STATE);

        _ = await _bankAccountReadRepository.GetByIdAsync(request.BankAccountId, request.VahedCode, cancellationToken)
            ?? throw new NotFoundException("BankAccount", request.BankAccountId);

        statement.BANK_ACCOUNT_ID = request.BankAccountId;
        statement.FROM_DATE = request.FromDate;
        statement.TO_DATE = request.ToDate;
        statement.CLOSING_BALANCE = request.ClosingBalance;
        statement.DESCRIPTION = request.Description;
        statement.CHANGEUSERID = _currentUser.UserId;
        statement.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
