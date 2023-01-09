using GitObjectDb.Model;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

namespace GitObjectDb.Api.ProtoBuf.Tests.Assets;

internal static class ConnectionProvider
{
    internal static string ReposPath => Path.Combine(TestContext.CurrentContext.WorkDirectory, "Repos");

    internal static string GetConnectionPath(string folder) => Path.Combine(ReposPath, folder);

    internal static IServiceCollection AddGitObjectDbConnection(this IServiceCollection services,
                                                                string folder, Action<IConnection>? populateData = null) =>
        services
        .AddSingleton(p => GetOrCreateConnection(p, folder, populateData))
        .AddSingleton<IQueryAccessor>(s => s.GetRequiredService<IConnection>());

    internal static IConnection GetOrCreateConnection(IServiceProvider provider, string folder, Action<IConnection>? populateData = null)
    {
        var path = GetConnectionPath(folder);
        var alreadyExists = Directory.Exists(path);
        var model = provider.GetRequiredService<IDataModel>();
        var repositoryFactory = provider.GetRequiredService<ConnectionFactory>();
        var result = repositoryFactory(path, model);
        if (!alreadyExists)
        {
            populateData?.Invoke(result);
        }
        return result;
    }
}
