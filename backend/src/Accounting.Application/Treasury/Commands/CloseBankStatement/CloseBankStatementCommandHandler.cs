using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Treasury.Commands.Common;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Commands.CloseBankStatement;

public sealed class CloseBankStatementCommandHandler : IRequestHandler<CloseBankStatementCommand>
{
    private static readonly TreasuryRole[] EditorRoles = { TreasuryRole.Treasurer, TreasuryRole.SeniorAccountant };

    private readonly ITreasuryBankStatementRepository _statementRepository;
    private readonly ITreasuryRoleAuthorizer _roleAuthorizer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CloseBankStatementCommandHandler(
        ITreasuryBankStatementRepository statementRepository,
        ITreasuryRoleAuthorizer roleAuthorizer,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _statementRepository = statementRepository;
        _roleAuthorizer = roleAuthorizer;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(CloseBankStatementCommand request, CancellationToken cancellationToken)
    {
        await _roleAuthorizer.EnsureHasRoleAsync(request.VahedCode, EditorRoles, cancellationToken);

        var statement = await _statementRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken);

        if (statement is null || statement.ISDELETED)
        {
            throw new NotFoundException("BankStatement", request.Id);
        }

        BankStatementEditability.EnsureOpen(statement.ID, statement.STATE);

        statement.STATE = BankStatementState.Closed;
        statement.CHANGEUSERID = _currentUser.UserId;
        statement.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
