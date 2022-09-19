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
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The source <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddGitObjectDb(this IServiceCollection source, Action<IGitObjectDbBuilder> configure)
    {
        // Avoid double-registrations
        if (source.IsGitObjectDbRegistered())
        {
            throw new NotSupportedException("GitObjectDb has already been registered.");
        }

        configure(new GitObjectDbBuilder(source));
        if (!source.Any(sd => sd.ServiceType == typeof(INodeSerializer.Factory)))
        {
            throw new GitObjectDbException(
                $"The {nameof(INodeSerializer)}.{nameof(INodeSerializer.Factory)} " +
                $"has not been configured. Consider using {nameof(AddGitObjectDb)}(" +
                $"c => c.{nameof(GitObjectDbBuilderExtensions.AddSerializer)}(...).");
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
        var internalFactories = typeof(Factories)
            .GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
            .Where(t => typeof(Delegate).IsAssignableFrom(t))
            .ToArray();
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
        source.AddSingleton<UpdateFastInsertFile>();
        source.AddSingleton<CommitCommand>();
        source.AddSingleton<FastImportCommitCommand>();
        source.AddSingleton<ServiceResolver<CommitCommandType, ICommitCommand>>(serviceProvider => type =>
        {
            if (type == CommitCommandType.Auto)
            {
                type = GitCliCommand.IsGitInstalled ? CommitCommandType.FastImport : CommitCommandType.Normal;
            }
            return type switch
            {
                CommitCommandType.Normal => serviceProvider.GetRequiredService<CommitCommand>(),
                CommitCommandType.FastImport => serviceProvider.GetRequiredService<FastImportCommitCommand>(),
                _ => throw new NotSupportedException(),
            };
        });
    }
}
