using FluentValidation;

namespace Accounting.Application.AttribForAccountCodes.Queries.GetAttribForAccountCodes;

/// <summary>
/// Surface-level (syntactic) pagination validation only.
/// </summary>
public sealed class GetAttribForAccountCodesQueryValidator : AbstractValidator<GetAttribForAccountCodesQuery>
{
    public const int MaxPageSize = 200;

    /// <summary>
    /// Upper bound for <see cref="GetAttribForAccountCodesQuery.PageNumber"/>. Chosen as
    /// <c>int.MaxValue / MaxPageSize</c> so that, for any allowed <c>PageSize</c> (up to
    /// <see cref="MaxPageSize"/>), the repository's <c>(pageNumber - 1) * pageSize</c>
    /// computation in <c>Skip(...)</c> can never overflow <see cref="int"/>. Do not remove
    /// without re-checking that overflow guarantee.
    /// </summary>
    public const int MaxPageNumber = int.MaxValue / MaxPageSize;

    public GetAttribForAccountCodesQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .InclusiveBetween(1, MaxPageNumber);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize);

        // Mirror the Fluent mapping constraints on the filtered columns: ACCCODE is
        // VARCHAR2(6), YEAR VARCHAR2(4). A longer bound could never match a row, so it is a
        // caller error rather than an empty result. (Same reasoning as
        // GetVoucherHeadsQueryValidator.)
        RuleFor(x => x.MoinCodeFrom).MaximumLength(6);
        RuleFor(x => x.MoinCodeTo).MaximumLength(6);
        RuleFor(x => x.Year).MaximumLength(4);

        // An inverted range returns nothing and almost always means the user filled the two
        // fields the wrong way round — report it instead of silently showing an empty table.
        //
        // Ordinal comparison is the right one here and needs no length handling: a معین ACCCODE
        // is always exactly 6 digits (docs/centralaccount-business-reference.md §۲-۱), unlike
        // DOC_NUM on the voucher list.
        RuleFor(x => x.MoinCodeTo)
            .Must((query, to) => string.CompareOrdinal(query.MoinCodeFrom, to) <= 0)
            .When(x => !string.IsNullOrEmpty(x.MoinCodeFrom) && !string.IsNullOrEmpty(x.MoinCodeTo))
            .WithMessage("'تا معین' نباید کوچک‌تر از 'از معین' باشد.");

        // Null passes (filter not supplied); only an out-of-range underlying integer is rejected.
        RuleFor(x => x.AttribSum).IsInEnum();
        RuleFor(x => x.Flag).IsInEnum();
    }
}
