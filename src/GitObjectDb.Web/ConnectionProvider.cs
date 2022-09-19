using GitObjectDb.Model;
using LibGit2Sharp;

namespace GitObjectDb.Web;

internal static class ConnectionProvider
{
    internal static IDataModel Model { get; } =
        new ConventionBaseModelBuilder()
        .RegisterAssemblyTypes(typeof(Application).Assembly)
        .Build();

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
            software.CreateData("Initial commit", new Signature("foo", "foo@acme.com", DateTimeOffset.Now));
        }
        return result;
    }
}
