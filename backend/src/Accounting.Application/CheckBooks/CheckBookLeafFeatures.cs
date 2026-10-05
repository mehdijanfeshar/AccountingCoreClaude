using System.Text.Json.Serialization;
using Accounting.Application.ChequeBook;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using FluentValidation;
using MediatR;

namespace Accounting.Application.CheckBooks;

/// <summary>اوراق چک یک دسته‌چک، با وضعیت هر برگ و سندی که در آن به کار رفته.</summary>
public sealed record GetCheckBookLeavesQuery(Guid CheckBookId) : IRequest<IReadOnlyList<ChequeLeafDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary>ساخت اوراق جاافتاده برای دسته‌چک‌های قدیمی (پیش از این قابلیت، بی‌برگ ساخته شده‌اند). تعداد برگ تازه.</summary>
public sealed record GenerateCheckBookLeavesCommand(Guid CheckBookId) : IRequest<int>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class CheckBookLeafHandlers :
    IRequestHandler<GetCheckBookLeavesQuery, IReadOnlyList<ChequeLeafDto>>,
    IRequestHandler<GenerateCheckBookLeavesCommand, int>
{
    private readonly ICheckBookRepository _checkBooks;
    private readonly IChequeBookRepository _cheques;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CheckBookLeafHandlers(
        ICheckBookRepository checkBooks, IChequeBookRepository cheques, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _checkBooks = checkBooks;
        _cheques = cheques;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ChequeLeafDto>> Handle(GetCheckBookLeavesQuery request, CancellationToken cancellationToken)
    {
        var book = await _checkBooks.GetForUpdateAsync(request.CheckBookId, request.VahedCode, cancellationToken);
        if (book is null || book.ISDELETED)
            throw new NotFoundException("CheckBook", request.CheckBookId);
        return await _cheques.GetLeavesAsync(book.ID, cancellationToken);
    }

    public async Task<int> Handle(GenerateCheckBookLeavesCommand request, CancellationToken cancellationToken)
    {
        var book = await _checkBooks.GetForUpdateAsync(request.CheckBookId, request.VahedCode, cancellationToken);
        if (book is null || book.ISDELETED)
            throw new NotFoundException("CheckBook", request.CheckBookId);

        if (CheckBookLeaves.IsSori(book.CHECKBOOK_TYPE))
            throw new ChequeConflictException("دسته‌چک صوری برگ از پیش ندارد؛ برگ‌ها هنگام استفاده در سند ساخته می‌شوند.");

        var from = book.FROMCHECKNUMBER;
        var to = book.TOCHECKNUMBER;
        if (!from.All(char.IsAsciiDigit) || !to.All(char.IsAsciiDigit) || from.Length != to.Length
            || from.Length > CheckBookLeaves.MaxNumberLength || long.Parse(from) > long.Parse(to)
            || long.Parse(to) - long.Parse(from) + 1 > CheckBookLeaves.MaxLeaves)
        {
            throw new ChequeConflictException(
                "بازهٔ شمارهٔ این دسته‌چک نامعتبر است (رقمی، هم‌طول، اولین ≤ آخرین، حداکثر ۱۰۰۰ برگ)؛ ابتدا دسته‌چک را اصلاح کنید.");
        }

        var existing = (await _checkBooks.GetLeavesForUpdateAsync(book.ID, cancellationToken)).Select(c => c.CHEQ_NO).ToList();
        var added = CheckBookLeaves.Generate(book, existing, _currentUser.UserId, DateTime.UtcNow);
        if (added > 0)
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        return added;
    }
}

public sealed class GenerateCheckBookLeavesCommandValidator : AbstractValidator<GenerateCheckBookLeavesCommand>
{
    public GenerateCheckBookLeavesCommandValidator() => RuleFor(x => x.CheckBookId).NotEmpty();
}
