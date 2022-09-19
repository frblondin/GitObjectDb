using GitObjectDb.Model;
using LibGit2Sharp;
using Models.Software;
using System.Reflection;

namespace GitObjectDb.Web
{
    internal static class ConnectionProvider
    {
        internal static IDataModel Model { get; } =
            new ConventionBaseModelBuilder()
            .RegisterAssemblyTypes(typeof(Application).Assembly)
            .Build();

        internal static IConnection GetOrCreateConnection(IServiceProvider provider)
        {
            var path = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new NotSupportedException("Assembly location could not be found."),
                "Repository");
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
}
