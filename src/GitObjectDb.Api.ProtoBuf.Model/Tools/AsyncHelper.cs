using System.Diagnostics.CodeAnalysis;

namespace GitObjectDb.Api.ProtoBuf.Model.Tools;

[ExcludeFromCodeCoverage]
internal static class AsyncHelper
{
    /// <summary>
    /// Executes an async method synchronously.
    /// This method uses Task.Run to avoid deadlocks in certain contexts.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="func">The async function to execute.</param>
    /// <returns>The result of the async operation.</returns>
    public static TResult RunSync<TResult>(Func<Task<TResult>> func)
    {
        var originalContext = SynchronizationContext.Current;
        try
        {
            SynchronizationContext.SetSynchronizationContext(null);
            return Task.Run(async () => await func().ConfigureAwait(false)).GetAwaiter().GetResult();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }
    }

    /// <summary>
    /// Executes an async method synchronously.
    /// This method uses Task.Run to avoid deadlocks in certain contexts.
    /// </summary>
    /// <param name="func">The async function to execute.</param>
    public static void RunSync(Func<Task> func)
    {
        var originalContext = SynchronizationContext.Current;
        try
        {
            SynchronizationContext.SetSynchronizationContext(null);
            Task.Run(async () => await func().ConfigureAwait(false)).GetAwaiter().GetResult();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }
    }
}