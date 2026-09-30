using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Treasury.Commands.Common;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Commands.MatchBankStatementLine;

public sealed class MatchBankStatementLineCommandHandler : IRequestHandler<MatchBankStatementLineCommand>
{
    private static readonly TreasuryRole[] EditorRoles = { TreasuryRole.Treasurer, TreasuryRole.SeniorAccountant };

    private readonly ITreasuryBankStatementRepository _statementRepository;
    private readonly ITreasuryBankStatementLineRepository _lineRepository;
    private readonly IBankStatementManualMatchService _matchService;
    private readonly ITreasuryRoleAuthorizer _roleAuthorizer;
    private readonly IUnitOfWork _unitOfWork;

    public MatchBankStatementLineCommandHandler(
        ITreasuryBankStatementRepository statementRepository,
        ITreasuryBankStatementLineRepository lineRepository,
        IBankStatementManualMatchService matchService,
        ITreasuryRoleAuthorizer roleAuthorizer,
        IUnitOfWork unitOfWork)
    {
        _statementRepository = statementRepository;
        _lineRepository = lineRepository;
        _matchService = matchService;
        _roleAuthorizer = roleAuthorizer;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(MatchBankStatementLineCommand request, CancellationToken cancellationToken)
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

        await _matchService.MatchAsync(statement, line, request.VoucherDetailId, request.VahedCode, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
