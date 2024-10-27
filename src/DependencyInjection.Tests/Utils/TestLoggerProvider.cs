namespace DependencyInjection.Tests.Utils
{
    using System;
    using System.Collections.Concurrent;
    using Microsoft.Extensions.Logging;
    using Xunit;

    public class BeginScopeContext
    {
        public object Scope { get; set; }

        public string LoggerName { get; set; }
    }

    public class EndScopeContext
    {
        public object Scope { get; set; }

        public string LoggerName { get; set; }
    }

    public class WriteContext
    {
        public LogLevel LogLevel { get; set; }

        public EventId EventId { get; set; }

        public object State { get; set; }

        public Exception Exception { get; set; }

        public Func<object, Exception, string> Formatter { get; set; }

        public object Scope { get; set; }

        public string LoggerName { get; set; }

        public string Message
        {
            get
            {
                return Formatter(State, Exception);
            }
        }
    }

    public interface ITestSink
    {
        event Action<WriteContext> MessageLogged;

        event Action<BeginScopeContext> ScopeStarted;

        Func<WriteContext, bool> WriteEnabled { get; set; }

        Func<BeginScopeContext, bool> BeginEnabled { get; set; }

        IProducerConsumerCollection<BeginScopeContext> Scopes { get; set; }

        IProducerConsumerCollection<WriteContext> Writes { get; set; }

        void Write(WriteContext context);

        void Begin(BeginScopeContext context);
    }

    public class TestSink : ITestSink
    {
        private ConcurrentQueue<BeginScopeContext> _scopes;
        private ConcurrentQueue<WriteContext> _writes;

        public TestSink(
            Func<WriteContext, bool> writeEnabled = null,
            Func<BeginScopeContext, bool> beginEnabled = null)
        {
            WriteEnabled = writeEnabled;
            BeginEnabled = beginEnabled;

            _scopes = new ConcurrentQueue<BeginScopeContext>();
            _writes = new ConcurrentQueue<WriteContext>();
        }

        public Func<WriteContext, bool> WriteEnabled { get; set; }

        public Func<BeginScopeContext, bool> BeginEnabled { get; set; }

        public IProducerConsumerCollection<BeginScopeContext> Scopes { get => _scopes; set => _scopes = new ConcurrentQueue<BeginScopeContext>(value); }

        public IProducerConsumerCollection<WriteContext> Writes { get => _writes; set => _writes = new ConcurrentQueue<WriteContext>(value); }

        public event Action<WriteContext> MessageLogged;

        public event Action<BeginScopeContext> ScopeStarted;

        public void Write(WriteContext context)
        {
            if (WriteEnabled == null || WriteEnabled(context))
            {
                _writes.Enqueue(context);
            }
            MessageLogged?.Invoke(context);
        }

        public void Begin(BeginScopeContext context)
        {
            if (BeginEnabled == null || BeginEnabled(context))
            {
                _scopes.Enqueue(context);
            }
            ScopeStarted?.Invoke(context);
        }

        public static bool EnableWithTypeName<T>(WriteContext context)
        {
            return context.LoggerName.Equals(typeof(T).FullName);
        }

        public static bool EnableWithTypeName<T>(BeginScopeContext context)
        {
            return context.LoggerName.Equals(typeof(T).FullName);
        }
    }

    public class TestLoggerProvider : ILoggerProvider
    {
        private readonly ITestSink _sink;

        public TestLoggerProvider(ITestSink sink)
        {
            _sink = sink;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new TestLogger(categoryName, _sink, enabled: true);
        }

        public void Dispose()
        {
        }
    }

    public class TestLogger : ILogger
    {
        private object _scope;
        private readonly ITestSink _sink;
        private readonly string _name;
        private readonly Func<LogLevel, bool> _filter;

        public TestLogger(string name, ITestSink sink, bool enabled)
            : this(name, sink, _ => enabled)
        {
        }

        public TestLogger(string name, ITestSink sink, Func<LogLevel, bool> filter)
        {
            _sink = sink;
            _name = name;
            _filter = filter;
        }

        public string Name { get; set; }

        public IDisposable BeginScope<TState>(TState state)
        {
            var oldScope = _scope;
            var disposable = new DelegateDisposable<TState>(() =>
            {
                _scope = oldScope;
            });

            _scope = state;

            _sink.Begin(new BeginScopeContext()
            {
                LoggerName = _name,
                Scope = state,
            });

            return disposable;

        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            _sink.Write(new WriteContext()
            {
                LogLevel = logLevel,
                EventId = eventId,
                State = state,
                Exception = exception,
                Formatter = (s, e) => formatter((TState)s, e),
                LoggerName = _name,
                Scope = _scope
            });
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None && _filter(logLevel);
        }

        private class TestDisposable : IDisposable
        {
            public static readonly TestDisposable Instance = new TestDisposable();

            public TestDisposable()
            {
            }
            public void Dispose()
            {
                // intentionally does nothing
            }
        }

        private class DelegateDisposable<TScope> : IDisposable
        {
            private readonly Action _onDispose;

            public DelegateDisposable(Action onDispose)
            {
                _onDispose = onDispose;
            }
            public void Dispose()
            {
                _onDispose?.Invoke();
            }
        }
    }
}
