using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Reports.AccountJournal.GetAccountJournal;

/// <summary>
/// Delegates straight to the read repository. Read-side handlers never touch
/// <c>IUnitOfWork</c> — there is nothing to persist.
/// </summary>
public sealed class GetAccountJournalQueryHandler
    : IRequestHandler<GetAccountJournalQuery, AccountJournalResultDto>
{
    private readonly IAccountJournalReadRepository _readRepository;

    public GetAccountJournalQueryHandler(IAccountJournalReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<AccountJournalResultDto> Handle(
        GetAccountJournalQuery request,
        CancellationToken cancellationToken)
        => _readRepository.GetAsync(request, cancellationToken);
}
