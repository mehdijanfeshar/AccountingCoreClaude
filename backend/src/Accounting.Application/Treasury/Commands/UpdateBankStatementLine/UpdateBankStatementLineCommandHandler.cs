using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Treasury.Commands.Common;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Commands.UpdateBankStatementLine;

public sealed class UpdateBankStatementLineCommandHandler : IRequestHandler<UpdateBankStatementLineCommand>
{
    private static readonly TreasuryRole[] EditorRoles = { TreasuryRole.Treasurer, TreasuryRole.SeniorAccountant };

    private readonly ITreasuryBankStatementRepository _statementRepository;
    private readonly ITreasuryBankStatementLineRepository _lineRepository;
    private readonly ITreasuryRoleAuthorizer _roleAuthorizer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateBankStatementLineCommandHandler(
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

    public async Task Handle(UpdateBankStatementLineCommand request, CancellationToken cancellationToken)
    {
        await _roleAuthorizer.EnsureHasRoleAsync(request.VahedCode, EditorRoles, cancellationToken);

        var statement = await _statementRepository.GetForUpdateAsync(request.StatementId, request.VahedCode, cancellationToken);

        if (statement is null || statement.ISDELETED)
        {
            throw new NotFoundException("BankStatement", request.StatementId);
        }

        BankStatementEditability.EnsureOpen(statement.ID, statement.STATE);

        var line = await _lineRepository.GetForUpdateAsync(request.LineId, statement.ID, cancellationToken);

        if (line is null || line.ISDELETED)
        {
            throw new NotFoundException("BankStatementLine", request.LineId);
        }

        if (line.MATCH_STATE != BankStatementLineMatchState.Unmatched)
        {
            throw new TreasuryBankStatementLineStateConflictException(line.ID, line.MATCH_STATE, "نامنطبق");
        }

        line.LINE_DATE = request.LineDate;
        line.BANK_REFERENCE = request.BankReference;
        line.DESCRIPTION = request.Description;
        line.WITHDRAWAL = request.Withdrawal;
        line.DEPOSIT = request.Deposit;
        line.BALANCE = request.Balance;
        line.CHANGEUSERID = _currentUser.UserId;
        line.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
