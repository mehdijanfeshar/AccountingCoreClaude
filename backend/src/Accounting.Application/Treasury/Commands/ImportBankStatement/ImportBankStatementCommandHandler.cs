using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Treasury.Commands.Common;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Commands.ImportBankStatement;

/// <summary>
/// Resolves <see cref="IBankStatementFileParser"/> via <see cref="IServiceProvider"/> — deliberately
/// NOT constructor-injected, because as of authoring time (۲۰۲۶-۰۹-۲۹) no implementation is
/// registered, and a required constructor dependency with nothing bound would break DI composition
/// at application startup. See <see cref="IBankStatementFileParser"/> XML doc.
/// </summary>
public sealed class ImportBankStatementCommandHandler : IRequestHandler<ImportBankStatementCommand, int>
{
    private static readonly TreasuryRole[] EditorRoles = { TreasuryRole.Treasurer, TreasuryRole.SeniorAccountant };

    private readonly ITreasuryBankStatementRepository _statementRepository;
    private readonly ITreasuryBankStatementLineRepository _lineRepository;
    private readonly ITreasuryRoleAuthorizer _roleAuthorizer;
    private readonly IServiceProvider _serviceProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public ImportBankStatementCommandHandler(
        ITreasuryBankStatementRepository statementRepository,
        ITreasuryBankStatementLineRepository lineRepository,
        ITreasuryRoleAuthorizer roleAuthorizer,
        IServiceProvider serviceProvider,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _statementRepository = statementRepository;
        _lineRepository = lineRepository;
        _roleAuthorizer = roleAuthorizer;
        _serviceProvider = serviceProvider;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(ImportBankStatementCommand request, CancellationToken cancellationToken)
    {
        await _roleAuthorizer.EnsureHasRoleAsync(request.VahedCode, EditorRoles, cancellationToken);

        var parser = (IBankStatementFileParser?)_serviceProvider.GetService(typeof(IBankStatementFileParser));

        if (parser is null)
        {
            throw new TreasuryBankStatementParserNotConfiguredException();
        }

        var statement = await _statementRepository.GetForUpdateAsync(request.StatementId, request.VahedCode, cancellationToken);

        if (statement is null || statement.ISDELETED)
        {
            throw new NotFoundException("BankStatement", request.StatementId);
        }

        BankStatementEditability.EnsureOpen(statement.ID, statement.STATE);

        await using var stream = new MemoryStream(request.Content);

        var parsedLines = await parser.ParseAsync(stream, statement.BANK_ACCOUNT_ID, cancellationToken);

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        foreach (var parsed in parsedLines)
        {
            var line = new TB_TR_BANK_STATEMENT_LINE
            {
                ID = Guid.NewGuid(),
                STATEMENT_ID = statement.ID,
                LINE_DATE = parsed.LineDate,
                BANK_REFERENCE = parsed.BankReference,
                DESCRIPTION = parsed.Description,
                WITHDRAWAL = parsed.Withdrawal,
                DEPOSIT = parsed.Deposit,
                BALANCE = parsed.Balance,
                MATCH_STATE = BankStatementLineMatchState.Unmatched,
                VAHEDCODE = request.VahedCode,
                YEAR = statement.YEAR,
                ADDUSERID = userId,
                CREATEDDATE = now,
                ISDELETED = false,
            };

            await _lineRepository.AddAsync(line, cancellationToken);
        }

        // SOURCE reflects how the CURRENT set of lines got here — an import replaces the "how"
        // even if earlier lines on this statement were entered manually (documented choice, since
        // TB_TR_BANK_STATEMENT.SOURCE is a single column, not a per-line one).
        statement.SOURCE = BankStatementSource.Import;
        statement.CHANGEUSERID = userId;
        statement.UPDATEDDATE = now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return parsedLines.Count;
    }
}
