using GitObjectDb.Comparison;
using GitObjectDb.Injection;
using GitObjectDb.Internal;
using GitObjectDb.Internal.Commands;
using GitObjectDb.Internal.Queries;
using GitObjectDb.Model;
using GitObjectDb.Tools;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace GitObjectDb;

/// <summary>A set of methods for instances of <see cref="IServiceCollection"/>.</summary>
public static class ServiceConfiguration
{
    /// <summary>Adds access to GitObjectDb repositories.</summary>
    /// <param name="source">The source.</param>
    /// <returns>The source <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddGitObjectDb(this IServiceCollection source)
    {
        // Avoid double-registrations
        if (source.IsGitObjectDbRegistered())
        {
            throw new NotSupportedException("GitObjectDb has already been registered.");
        }

        ConfigureMain(source);
        ConfigureQueries(source);
        ConfigureCommands(source);

        return source;
    }

    /// <summary>Gets whether GitObjectDb has already been registered.</summary>
    /// <param name="source">The source.</param>
    /// <returns><c>true</c> if the service has already been registered, <c>false</c> otherwise.</returns>
    private static bool IsGitObjectDbRegistered(this IServiceCollection source) =>
        source.Any(sd => sd.ServiceType == typeof(ConnectionFactory));

    private static void ConfigureMain(IServiceCollection source)
    {
        source.AddFactoryDelegate<ConnectionFactory, Connection>();
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
        var internalFactories = typeof(Factories)
            .GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
            .Where(t => typeof(Delegate).IsAssignableFrom(t))
            .ToArray();
#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
        source.AddFactoryDelegates(typeof(Connection).Assembly, internalFactories);
        source.AddSingleton<Comparer>();
        source.AddSingleton<IComparer>(s => s.GetRequiredService<Comparer>());
        source.AddSingleton<IComparerInternal>(s => s.GetRequiredService<Comparer>());
        source.AddSingleton<IMergeComparer, MergeComparer>();
        source.AddSingleton<Func<ITreeValidation>>(() => new TreeValidation());
    }

    private static void ConfigureQueries(IServiceCollection source)
    {
        source.AddServicesImplementing(typeof(QueryItems).Assembly, typeof(IQuery<,>), ServiceLifetime.Singleton);
    }

    private static void ConfigureCommands(IServiceCollection source)
    {
        source.AddSingleton<ICommitCommand, CommitCommand>();
        source.AddSingleton<IGitUpdateCommand, GitUpdateCommand>();
    }
}
