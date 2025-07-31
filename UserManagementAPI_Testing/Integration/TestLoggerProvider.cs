using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace UserManagementAPI_Testing.Integration
{
    public class TestLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentQueue<string> _logs = new();

        public ILogger CreateLogger(string categoryName)
        {
            return new TestLogger(categoryName, _logs);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }

        public List<string> GetLogs()
        {
            return _logs.ToList();
        }

        public void Clear()
        {
            // Clear all items from the queue
            while (_logs.TryDequeue(out _))
            {
                // Keep dequeuing until empty
            }
        }
    }

    public class TestLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly ConcurrentQueue<string> _logs;

        public TestLogger(string categoryName, ConcurrentQueue<string> logs)
        {
            _categoryName = categoryName;
            _logs = logs;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            var logEntry = $"[{logLevel}] {_categoryName}: {message}";
            
            if (exception != null)
            {
                logEntry += $" Exception: {exception}";
            }
            
            _logs.Enqueue(logEntry);
        }
    }
}