using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Cvitlo.Resumes.Application.IntegrationTests;

public sealed class CollectingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<string> _entries = new();

    // One string per log call: the rendered message plus every structured value and the exception text.
    public IReadOnlyCollection<string> Entries => _entries;

    public ILogger CreateLogger(string categoryName) => new CollectingLogger(_entries);

    public void Dispose()
    {
    }

    private sealed class CollectingLogger(ConcurrentQueue<string> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var entry = new StringBuilder(formatter(state, exception));

            if (state is IEnumerable<KeyValuePair<string, object?>> values)
            {
                foreach (var value in values)
                {
                    entry.Append('\n').Append(value.Value);
                }
            }

            if (exception is not null)
            {
                entry.Append('\n').Append(exception);
            }

            entries.Enqueue(entry.ToString());
        }
    }
}
