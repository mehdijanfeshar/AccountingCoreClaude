namespace Accounting.Application.VahedTypes.Queries;

/// <summary>
/// Read-side projection of <c>TB_VAHED_TYPE</c> — the 17 organisational unit types that
/// <c>TB_WHITEANDBLACKLIST</c> grants permissions against, and that every
/// <c>TB_VAHED_INFO</c> row belongs to through its non-nullable <c>VAHEDTYPE_ID</c>.
/// </summary>
/// <param name="Id">ID column — what the allow/deny-list rows reference.</param>
/// <param name="TypeCode">TYPECODE column.</param>
/// <param name="TypeName">TYPENAME column — the user-facing name («بیمارستان», «خزانه», …).</param>
/// <param name="ParentTypeCode">
/// PARENTTYPECODE column: the bucket this type belongs to, which the «دسترسی کدینگ حسابداری»
/// screen renders as its «بخش» grouping level.
///
/// ⚠️ It is <b>not</b> a self-reference to another <c>TYPECODE</c> — live data rules that out
/// (type 3 «بيمارستان» has parent "2", and type 12 «خزانه» has parent "3", neither of which names
/// a plausible parent type). It is a bare bucket id with no lookup table anywhere in the schema,
/// and the reference project's own tree endpoint returns it unlabelled
/// (<c>GetVahedTypeQueryTreeHandler</c> sets <c>MainTypeGroupDto.label = mainGroup.Key</c> and no
/// display value). Naming the buckets is therefore left to the presentation layer; see the
/// recorded assumption in <c>docs/open-decisions.md</c>.
/// </param>
public sealed record VahedTypeDto(
    Guid Id,
    string? TypeCode,
    string? TypeName,
    string? ParentTypeCode);
