using FluentValidation;

namespace Accounting.Application.WhiteAndBlackLists.Commands.BlacklistWhiteAndBlackList;

/// <summary>
/// Surface-level (syntactic) validation only. The row's existence is not checked here — that is
/// the handler's job, because only it can distinguish "missing" from "soft-deleted" and both map
/// to 404.
/// </summary>
public sealed class BlacklistWhiteAndBlackListCommandValidator : AbstractValidator<BlacklistWhiteAndBlackListCommand>
{
    public BlacklistWhiteAndBlackListCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);
    }
}
