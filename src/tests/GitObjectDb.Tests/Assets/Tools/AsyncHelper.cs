using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using NUnit.Framework;

namespace System.Threading.Tasks;

[ExcludeFromCodeCoverage]
public static class AsyncHelper
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

/// <summary>
/// Tests for AsyncHelper to ensure it doesn't deadlock under various scenarios.
/// </summary>
[TestFixture]
[ExcludeFromCodeCoverage]
public class AsyncHelperTests
{
    [Test]
    public void RunSync_WithSimpleTask_ReturnsValue()
    {
        // Act
        var result = AsyncHelper.RunSync(async () =>
        {
            await Task.Delay(10);
            return 42;
        });

        // Assert
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void RunSync_WithVoidTask_DoesNotDeadlock()
    {
        // Act & Assert (should not hang)
        Assert.DoesNotThrow(() =>
        {
            AsyncHelper.RunSync(async () =>
            {
                await Task.Delay(10);
            });
        });
    }

    [Test]
    public void RunSync_WithNestedCalls_DoesNotDeadlock()
    {
        // Act & Assert (should not hang)
        Assert.DoesNotThrow(() =>
        {
            AsyncHelper.RunSync(async () =>
            {
                await Task.Delay(10);
                AsyncHelper.RunSync(async () =>
                {
                    await Task.Delay(10);
                });
            });
        });
    }

    [Test]
    public void RunSync_WithSynchronizationContext_DoesNotDeadlock()
    {
        // Arrange
        var originalContext = SynchronizationContext.Current;
        try
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

            // Act & Assert (should not hang)
            Assert.DoesNotThrow(() =>
            {
                AsyncHelper.RunSync(async () =>
                {
                    await Task.Delay(10);
                });
            });
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }
    }
}