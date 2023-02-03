using System;
using System.Threading;

namespace GitObjectDb.SystemTextJson;

/// <summary>Context of serialization service.</summary>
public static class NodeSerializerContext
{
    private static readonly AsyncLocal<IServiceProvider> _serviceProvider = new();

    /// <summary>
    /// Gets or sets the current <see cref="IServiceProvider"/> to be used to
    /// resolve dependencies if any while deserializing nodes.
    /// </summary>
    public static IServiceProvider ServiceProvider
    {
        get => _serviceProvider.Value ?? throw new NotSupportedException("No scoped service provider created.");
        set => _serviceProvider.Value = value;
    }
}
