using GitObjectDb.Model;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.IO;

namespace GitObjectDb.Api.GraphQL.Tests.Assets;

internal static class ConnectionProvider
{
    internal static string ReposPath => Path.Combine(TestContext.CurrentContext.WorkDirectory, "Repos");

    internal static IServiceCollection AddGitObjectDbConnection(this IServiceCollection services, IDataModel model,
                                                                string folder, Action<IConnection>? populateData = null) => services
        .AddSingleton(p => GetOrCreateConnection(p, model, folder, populateData))
        .AddSingleton<IQueryAccessor>(s => s.GetRequiredService<IConnection>());

    internal static IConnection GetOrCreateConnection(IServiceProvider provider, IDataModel model, string folder,
                                                      Action<IConnection>? populateData = null)
    {
        var path = Path.Combine(ReposPath, folder);
        var alreadyExists = Directory.Exists(path);
        var repositoryFactory = provider.GetRequiredService<ConnectionFactory>();
        var result = repositoryFactory(path, model);
        if (!alreadyExists)
        {
            populateData?.Invoke(result);
        }
        return result;
    }
}
