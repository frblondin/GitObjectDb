using GitObjectDb.Model;
using LibGit2Sharp;

namespace GitObjectDb.Web;

internal static class ConnectionProvider
{
    internal static IServiceCollection AddGitObjectDbConnection(this IServiceCollection services, string folder, Action<IConnection>? populateData = null)
    {
        var model = services.FirstOrDefault(s => s.ServiceType == typeof(IDataModel) &&
            s.Lifetime == ServiceLifetime.Singleton &&
            s.ImplementationInstance is not null)?.ImplementationInstance as IDataModel ??
            throw new NotSupportedException($"{nameof(IDataModel)} has not bee registered.");

        return services
            .AddSingleton(p => GetOrCreateConnection(p, model, folder, populateData))
            .AddSingleton<IQueryAccessor>(s => s.GetRequiredService<IConnection>());
    }

    internal static IConnection GetOrCreateConnection(IServiceProvider provider, IDataModel model, string folder, Action<IConnection>? populateData = null)
    {
        var assemblyDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location) ??
            throw new NotSupportedException("Assembly location could not be found.");
        var path = Path.Combine(assemblyDirectory, folder);
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
