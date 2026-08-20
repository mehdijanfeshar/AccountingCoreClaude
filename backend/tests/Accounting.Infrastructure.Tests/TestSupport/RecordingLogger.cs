using Microsoft.Extensions.Logging;

namespace Accounting.Infrastructure.Tests.TestSupport;

/// <summary>
/// Captures every formatted log message (and, if present, the logged exception's full
/// <see cref="Exception.ToString"/>) so tests can assert that a secret/token value never appears
/// anywhere in logged output (defect fix #6), without depending on a specific logging framework.
/// </summary>
internal sealed class RecordingLogger<T> : ILogger<T>
{
    public List<string> Messages { get; } = new();

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Messages.Add(formatter(state, exception));

        if (exception is not null)
        {
            Messages.Add(exception.ToString());
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
