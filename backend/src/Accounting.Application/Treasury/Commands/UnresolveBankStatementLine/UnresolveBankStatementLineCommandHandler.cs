using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Treasury.Commands.Common;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Commands.UnresolveBankStatementLine;

/// <summary>Explicit transaction boundary — may soft-delete a temporary GL voucher (see
/// <c>IBankStatementLineResolutionService.UnresolveAsync</c> XML doc). Same posture as
/// <c>ResolveBankStatementLineCommandHandler</c>.</summary>
public sealed class UnresolveBankStatementLineCommandHandler : IRequestHandler<UnresolveBankStatementLineCommand>
{
    private static readonly TreasuryRole[] EditorRoles = { TreasuryRole.Treasurer, TreasuryRole.SeniorAccountant };

    private readonly ITreasuryBankStatementRepository _statementRepository;
    private readonly ITreasuryBankStatementLineRepository _lineRepository;
    private readonly IBankStatementLineResolutionService _resolutionService;
    private readonly ITreasuryRoleAuthorizer _roleAuthorizer;
    private readonly IUnitOfWork _unitOfWork;

    public UnresolveBankStatementLineCommandHandler(
        ITreasuryBankStatementRepository statementRepository,
        ITreasuryBankStatementLineRepository lineRepository,
        IBankStatementLineResolutionService resolutionService,
        ITreasuryRoleAuthorizer roleAuthorizer,
        IUnitOfWork unitOfWork)
    {
        _statementRepository = statementRepository;
        _lineRepository = lineRepository;
        _resolutionService = resolutionService;
        _roleAuthorizer = roleAuthorizer;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UnresolveBankStatementLineCommand request, CancellationToken cancellationToken)
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

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            await _resolutionService.UnresolveAsync(line, request.VahedCode, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
