using MediatR;

namespace Accounting.Application.Rabets.Queries.GetRabetTypes;

/// <summary>
/// انواع حساب رابط (<c>TB_RABET_TYPE</c>) برای صفحهٔ تعریف رابط. کد ۱/۲/۳ همان نوع اعلامیه است
/// (<c>ElamWorkflowService.RabetTypeCode</c>): صادره، رسیده، صادرهٔ درآمد. عنوان ذخیره‌شدهٔ نوع ۱ با این
/// معنا نمی‌خواند (ریسک #۳۱)، پس فرانت برچسب را از کد می‌سازد و عنوان جدول را فقط کنارش نشان می‌دهد.
/// </summary>
public sealed record GetRabetTypesQuery : IRequest<IReadOnlyList<RabetTypeDto>>;

public sealed record RabetTypeDto(Guid Id, string? Code, string? Title);

public interface IRabetTypeReadRepository
{
    Task<IReadOnlyList<RabetTypeDto>> ListAsync(CancellationToken ct);
}

public sealed class GetRabetTypesQueryHandler(IRabetTypeReadRepository repo) : IRequestHandler<GetRabetTypesQuery, IReadOnlyList<RabetTypeDto>>
{
    public Task<IReadOnlyList<RabetTypeDto>> Handle(GetRabetTypesQuery request, CancellationToken cancellationToken) => repo.ListAsync(cancellationToken);
}
