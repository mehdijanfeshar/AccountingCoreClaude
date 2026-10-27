using Accounting.Application.OperationTemplates.Agent;
using Accounting.Domain.OperationTemplates;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.OperationTemplates;

/// <summary>گفتگو با دستیار (DDL 087). فقط stage؛ SaveChanges با هندلر.</summary>
public sealed class AgentConversationRepository : IAgentConversationRepository
{
    private readonly LegacyDbContext _db;
    public AgentConversationRepository(LegacyDbContext db) => _db = db;

    public Task<AgentConversation?> FindAsync(Guid id, CancellationToken ct) =>
        _db.Set<AgentConversation>().FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task AddConversationAsync(AgentConversation conversation, CancellationToken ct) =>
        await _db.Set<AgentConversation>().AddAsync(conversation, ct);

    public async Task<IReadOnlyList<AgentMessage>> ListMessagesAsync(Guid conversationId, CancellationToken ct) =>
        await _db.Set<AgentMessage>().AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.Seq)
            .ToListAsync(ct);

    public async Task AddMessageAsync(AgentMessage message, CancellationToken ct) =>
        await _db.Set<AgentMessage>().AddAsync(message, ct);

    public async Task<IReadOnlyList<AgentConversation>> ListMineAsync(string userId, string vahedCode, int take, CancellationToken ct) =>
        await _db.Set<AgentConversation>().AsNoTracking()
            .Where(c => c.CreatedBy == userId && c.VahedCode == vahedCode && !c.IsDeleted)
            .OrderByDescending(c => c.UpdatedAtUtc)
            .Take(take)
            .ToListAsync(ct);
}
