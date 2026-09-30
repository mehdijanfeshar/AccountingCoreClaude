using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Treasury.Commands.Common;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Commands.DeleteBankStatement;

public sealed class DeleteBankStatementCommandHandler : IRequestHandler<DeleteBankStatementCommand>
{
    private static readonly TreasuryRole[] EditorRoles = { TreasuryRole.Treasurer, TreasuryRole.SeniorAccountant };

    private readonly ITreasuryBankStatementRepository _statementRepository;
    private readonly ITreasuryBankStatementLineRepository _lineRepository;
    private readonly ITreasuryRoleAuthorizer _roleAuthorizer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DeleteBankStatementCommandHandler(
        ITreasuryBankStatementRepository statementRepository,
        ITreasuryBankStatementLineRepository lineRepository,
        ITreasuryRoleAuthorizer roleAuthorizer,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _statementRepository = statementRepository;
        _lineRepository = lineRepository;
        _roleAuthorizer = roleAuthorizer;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteBankStatementCommand request, CancellationToken cancellationToken)
    {
        await _roleAuthorizer.EnsureHasRoleAsync(request.VahedCode, EditorRoles, cancellationToken);

        var statement = await _statementRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken);

        if (statement is null || statement.ISDELETED)
        {
            throw new NotFoundException("BankStatement", request.Id);
        }

        BankStatementEditability.EnsureOpen(statement.ID, statement.STATE);

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        statement.ISDELETED = true;
        statement.CHANGEUSERID = userId;
        statement.UPDATEDDATE = now;

        await _lineRepository.SoftDeleteByStatementAsync(statement.ID, userId, now, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
