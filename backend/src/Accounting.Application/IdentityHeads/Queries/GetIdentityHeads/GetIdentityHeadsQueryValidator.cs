using FluentValidation;

namespace Accounting.Application.IdentityHeads.Queries.GetIdentityHeads;

/// <summary>
/// Surface-level (syntactic) pagination and filter validation only.
/// </summary>
public sealed class GetIdentityHeadsQueryValidator : AbstractValidator<GetIdentityHeadsQuery>
{
    public const int MaxPageSize = 200;

    /// <summary>
    /// Upper bound for <see cref="GetIdentityHeadsQuery.PageNumber"/>, chosen as
    /// <c>int.MaxValue / MaxPageSize</c> so the repository's <c>(pageNumber - 1) * pageSize</c>
    /// can never overflow <see cref="int"/>. Do not remove without re-checking that guarantee.
    /// </summary>
    public const int MaxPageNumber = int.MaxValue / MaxPageSize;

    public GetIdentityHeadsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .InclusiveBetween(1, MaxPageNumber);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize);

        RuleFor(x => x.Year)
            .MaximumLength(4);

        // Explicit `!= Guid.Empty` rather than .NotEmpty(): on a nullable Guid, FluentValidation's
        // NotEmpty() compares against default(Guid?) — null, not Guid.Empty — so an all-zero id
        // would slip through. Same trap as GetIdentitySubGroupsQueryValidator.
        RuleFor(x => x.IdentityGroupId)
            .Must(id => id != Guid.Empty)
            .When(x => x.IdentityGroupId.HasValue)
            .WithMessage("شناسهٔ گروه شناسنامه نمی‌تواند خالی باشد.");
    }
}
