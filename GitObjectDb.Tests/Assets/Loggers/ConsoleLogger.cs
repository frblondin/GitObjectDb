using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;

namespace GitObjectDb.Tests.Assets.Loggers
{
    public class ConsoleLogger : ILogger
    {
        private readonly string _name;
        private readonly IExternalScopeProvider _scopeProvider;

        public ConsoleLogger(string name, IExternalScopeProvider scopeProvider)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
        }

        public IDisposable BeginScope<TState>(TState state) => _scopeProvider.Push(state);

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var result = new StringBuilder()
                .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))
                .Append(" ")
                .Append(logLevel)
                .Append(" ")
                .Append(_name)
                .Append(" [")
                .Append(eventId)
                .Append("]");

            GetScopeInformation(result);

            var message = formatter(state, exception);
            result
                .Append("\n\t")
                .Append(message);

            Console.WriteLine(result.ToString());
        }

        private void GetScopeInformation(StringBuilder stringBuilder)
        {
            if (_scopeProvider != null)
            {
                _scopeProvider.ForEachScope((scope, sb) =>
                {
                    var empty = sb.Length == 0;
#if NETCOREAPP
                    var message = scope?.ToString().Replace("\n", string.Empty, StringComparison.OrdinalIgnoreCase) ?? "";
#else
                    var message = scope?.ToString().Replace("\n", string.Empty) ?? "";
#endif
                    sb.Append(empty ? "=> " : " => ").Append(message);
                }, stringBuilder);
            }
        }
    }
}
