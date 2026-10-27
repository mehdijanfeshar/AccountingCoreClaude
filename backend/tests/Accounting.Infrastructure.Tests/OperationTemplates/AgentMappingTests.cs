using Accounting.Domain.OperationTemplates;
using Accounting.Infrastructure.Legacy;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Tests.OperationTemplates;

/// <summary>
/// FKهای DDL 087/088 باید در مدل EF هم باشند؛ وگرنه EF ترتیب درج را نمی‌داند و پیام پیش از ردپا/گفتگو درج می‌شود
/// (ORA-02291 روی FK_OP_AGENT_MSG_TRACE، ۲۰۲۶-۱۰-۲۰).
/// </summary>
public sealed class AgentMappingTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly LegacyDbContext _context;

    public AgentMappingTests()
    {
        _connection.Open();
        _context = new LegacyDbContext(new DbContextOptionsBuilder<LegacyDbContext>().UseSqlite(_connection).Options);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Theory]
    [InlineData(typeof(AgentMessage), typeof(AgentConversation), "FK_OP_AGENT_MSG_CONV")]
    [InlineData(typeof(AgentMessage), typeof(OperationAiTrace), "FK_OP_AGENT_MSG_TRACE")]
    [InlineData(typeof(AiEvalItem), typeof(AiEvalRun), "FK_OP_AI_EVAL_ITEM_RUN")]
    public void ForeignKey_IsModelled_SoInsertOrderIsCorrect(Type dependent, Type principal, string name)
    {
        var fk = _context.Model.FindEntityType(dependent)!.GetForeignKeys()
            .SingleOrDefault(f => f.PrincipalEntityType.ClrType == principal);
        Assert.NotNull(fk);
        Assert.Equal(name, fk!.GetConstraintName());
    }
}
