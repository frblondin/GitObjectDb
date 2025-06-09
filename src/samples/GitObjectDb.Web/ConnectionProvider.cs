using GitObjectDb.Model;

namespace GitObjectDb.Web;

internal static class ConnectionProvider
{
    internal static async Task AddGitObjectDbConnectionAsync(this IServiceCollection services, string folder, Func<IConnection, Task>? populateData = null)
    {
        var connection = await GetOrCreateConnectionAsync(services.BuildServiceProvider(), folder, populateData);
        services.AddScoped<IConnection>(s => connection);
        services.AddScoped<IDataProvider>(s => connection);
    }

    private static async Task<IConnection> GetOrCreateConnectionAsync(IServiceProvider provider, string folder, Func<IConnection, Task>? populateData = null)
    {
        var assemblyDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location) ??
            throw new NotSupportedException("Assembly location could not be found.");
        var path = Path.Combine(assemblyDirectory, folder);
        var alreadyExists = Directory.Exists(path);
        var model = provider.GetRequiredService<IDataModel>();
        var repositoryFactory = provider.GetRequiredService<ConnectionFactory>();
        var result = repositoryFactory(path, model);
        if (!alreadyExists && populateData != null)
        {
            await populateData.Invoke(result);
        }
        return result;
    }
}
