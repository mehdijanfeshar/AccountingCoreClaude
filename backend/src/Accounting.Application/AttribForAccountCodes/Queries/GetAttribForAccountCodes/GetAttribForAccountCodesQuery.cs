using Accounting.Application.Common;
using MediatR;

namespace Accounting.Application.AttribForAccountCodes.Queries.GetAttribForAccountCodes;

/// <summary>
/// Returns a page of <c>TB_ATTRIBFORACCOUNTCODE</c> rows projected to
/// <see cref="AttribForAccountCodeDto"/>. Only non-deleted rows (<c>ISDELETED != true</c>) are
/// included.
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetAttribForAccountCodesQueryValidator.MaxPageSize"/>.</param>
public sealed record GetAttribForAccountCodesQuery(int PageNumber, int PageSize) : IRequest<PagedResult<AttribForAccountCodeDto>>;
