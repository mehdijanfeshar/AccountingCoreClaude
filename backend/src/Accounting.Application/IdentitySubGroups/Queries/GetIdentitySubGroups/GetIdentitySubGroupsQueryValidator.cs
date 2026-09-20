using FluentValidation;

namespace Accounting.Application.IdentitySubGroups.Queries.GetIdentitySubGroups;

/// <summary>
/// Surface-level (syntactic) pagination validation only.
/// </summary>
public sealed class GetIdentitySubGroupsQueryValidator : AbstractValidator<GetIdentitySubGroupsQuery>
{
    public const int MaxPageSize = 200;

    /// <summary>
    /// Upper bound for <see cref="GetIdentitySubGroupsQuery.PageNumber"/>. Chosen as
    /// <c>int.MaxValue / MaxPageSize</c> so that, for any allowed <c>PageSize</c> (up to
    /// <see cref="MaxPageSize"/>), the repository's <c>(pageNumber - 1) * pageSize</c>
    /// computation in <c>Skip(...)</c> can never overflow <see cref="int"/>. Do not remove
    /// without re-checking that overflow guarantee.
    /// </summary>
    public const int MaxPageNumber = int.MaxValue / MaxPageSize;

    public GetIdentitySubGroupsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .InclusiveBetween(1, MaxPageNumber);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize);

        // Both filters are optional: null passes (the filter simply is not applied). Only a
        // supplied-but-meaningless value is rejected.
        // Explicit `!= Guid.Empty` rather than .NotEmpty(): on a *nullable* Guid, FluentValidation's
        // NotEmpty() compares against default(Guid?) — which is null, not Guid.Empty — so an
        // all-zero Guid would slip straight through it. (Caught by
        // Validate_EmptyIdentityGroupId_Fails, which passed against the naive rule.)
        RuleFor(x => x.IdentityGroupId)
            .Must(id => id != Guid.Empty)
            .When(x => x.IdentityGroupId.HasValue)
            .WithMessage("شناسهٔ گروه شناسنامه نمی‌تواند خالی باشد.");

        RuleFor(x => x.Kind)
            .IsInEnum();
    }
}
