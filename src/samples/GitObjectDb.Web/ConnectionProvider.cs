using GitObjectDb.Model;
using LibGit2Sharp;

namespace GitObjectDb.Web;

internal static class ConnectionProvider
{
    internal static IServiceCollection AddGitObjectDbConnection(this IServiceCollection services, string folder, Action<IConnection>? populateData = null) => services
        .AddSingleton(p => GetOrCreateConnection(p, folder, populateData))
        .AddSingleton<IQueryAccessor>(s => s.GetRequiredService<IConnection>());

    internal static IConnection GetOrCreateConnection(IServiceProvider provider, string folder, Action<IConnection>? populateData = null)
    {
        var assemblyDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location) ??
            throw new NotSupportedException("Assembly location could not be found.");
        var path = Path.Combine(assemblyDirectory, folder);
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
