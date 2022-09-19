using GitObjectDb.Model;
using LibGit2Sharp;

namespace GitObjectDb.Web;

internal static class ConnectionProvider
{
    internal static IDataModel Model { get; } =
        new ConventionBaseModelBuilder()
        .RegisterAssemblyTypes(typeof(Application).Assembly)
        .Build();

    internal static IServiceCollection AddGitObjectDbConnection(this IServiceCollection services) => services
        .AddSingleton(GetOrCreateConnection)
        .AddSingleton<IQueryAccessor>(s => s.GetRequiredService<IConnection>());

    internal static IConnection GetOrCreateConnection(IServiceProvider provider)
    {
        var assemblyDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location) ??
            throw new NotSupportedException("Assembly location could not be found.");
        var path = Path.Combine(assemblyDirectory, "Repository");
        var alreadyExists = Directory.Exists(path);
        var repositoryFactory = provider.GetRequiredService<ConnectionFactory>();
        var result = repositoryFactory(path, Model);
        if (!alreadyExists)
        {
            var software = new DataGenerator(result);
            software.CreateData("Initial commit", new("foo", "foo@acme.com", DateTimeOffset.Now));

            var application = software.Connection.GetApplications().First();
            software.Connection
                .Update(c => c.CreateOrUpdate(application with { Description = "New description" }))
                .Commit(new("Update appication",
                            new("foo", "foo@acme.com", DateTimeOffset.Now),
                            new("foo", "foo@acme.com", DateTimeOffset.Now)));
        }
        return result;
    }
}
