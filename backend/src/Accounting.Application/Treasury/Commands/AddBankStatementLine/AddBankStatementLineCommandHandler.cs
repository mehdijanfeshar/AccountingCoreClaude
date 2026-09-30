using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Treasury.Commands.Common;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Commands.AddBankStatementLine;

public sealed class AddBankStatementLineCommandHandler : IRequestHandler<AddBankStatementLineCommand, Guid>
{
    private static readonly TreasuryRole[] EditorRoles = { TreasuryRole.Treasurer, TreasuryRole.SeniorAccountant };

    private readonly ITreasuryBankStatementRepository _statementRepository;
    private readonly ITreasuryBankStatementLineRepository _lineRepository;
    private readonly ITreasuryRoleAuthorizer _roleAuthorizer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public AddBankStatementLineCommandHandler(
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

    public async Task<Guid> Handle(AddBankStatementLineCommand request, CancellationToken cancellationToken)
    {
        await _roleAuthorizer.EnsureHasRoleAsync(request.VahedCode, EditorRoles, cancellationToken);

        var statement = await _statementRepository.GetForUpdateAsync(request.StatementId, request.VahedCode, cancellationToken);

        if (statement is null || statement.ISDELETED)
        {
            throw new NotFoundException("BankStatement", request.StatementId);
        }

        BankStatementEditability.EnsureOpen(statement.ID, statement.STATE);

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        var line = new TB_TR_BANK_STATEMENT_LINE
        {
            ID = Guid.NewGuid(),
            STATEMENT_ID = statement.ID,
            LINE_DATE = request.LineDate,
            BANK_REFERENCE = request.BankReference,
            DESCRIPTION = request.Description,
            WITHDRAWAL = request.Withdrawal,
            DEPOSIT = request.Deposit,
            BALANCE = request.Balance,
            MATCH_STATE = BankStatementLineMatchState.Unmatched,
            VAHEDCODE = request.VahedCode,
            YEAR = statement.YEAR,
            ADDUSERID = userId,
            CREATEDDATE = now,
            ISDELETED = false,
        };

        await _lineRepository.AddAsync(line, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return line.ID;
    }
}
