using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Tests.Assets.Loggers;

public sealed class ConsoleProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, ConsoleLogger> _loggers = new();
    private readonly IExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();

    public ILogger CreateLogger(string categoryName) =>
        _loggers.GetOrAdd(categoryName, _ => new ConsoleLogger(categoryName, _scopeProvider));

    public void Dispose()
    {
        // Not needed
    }
}
