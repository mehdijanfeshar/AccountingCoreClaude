using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Treasury.Commands.Common;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Commands.AutoMatchBankStatement;

public sealed class AutoMatchBankStatementCommandHandler : IRequestHandler<AutoMatchBankStatementCommand, BankStatementAutoMatchResult>
{
    private static readonly TreasuryRole[] EditorRoles = { TreasuryRole.Treasurer, TreasuryRole.SeniorAccountant };

    private readonly ITreasuryBankStatementRepository _statementRepository;
    private readonly IBankStatementAutoMatchService _autoMatchService;
    private readonly ITreasuryRoleAuthorizer _roleAuthorizer;
    private readonly IUnitOfWork _unitOfWork;

    public AutoMatchBankStatementCommandHandler(
        ITreasuryBankStatementRepository statementRepository,
        IBankStatementAutoMatchService autoMatchService,
        ITreasuryRoleAuthorizer roleAuthorizer,
        IUnitOfWork unitOfWork)
    {
        _statementRepository = statementRepository;
        _autoMatchService = autoMatchService;
        _roleAuthorizer = roleAuthorizer;
        _unitOfWork = unitOfWork;
    }

    public async Task<BankStatementAutoMatchResult> Handle(AutoMatchBankStatementCommand request, CancellationToken cancellationToken)
    {
        await _roleAuthorizer.EnsureHasRoleAsync(request.VahedCode, EditorRoles, cancellationToken);

        var statement = await _statementRepository.GetForUpdateAsync(request.StatementId, request.VahedCode, cancellationToken);

        if (statement is null || statement.ISDELETED)
        {
            throw new NotFoundException("BankStatement", request.StatementId);
        }

        BankStatementEditability.EnsureOpen(statement.ID, statement.STATE);

        var result = await _autoMatchService.AutoMatchAsync(statement, request.VahedCode, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return result;
    }
}
