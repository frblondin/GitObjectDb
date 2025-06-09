using AutoFixture;
using GitDotNet;
using GitObjectDb.Model;
using GitObjectDb.Tests.Assets.Tools;
using Microsoft.Extensions.DependencyInjection;
using Models.Software;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace GitObjectDb.Tests.Assets.Data.Software;

public class SoftwareCustomization(string repositoryPath) : IAsyncCustomization
{
    private static readonly ConcurrentDictionary<string, string> _templates = new();

    public SoftwareCustomization()
        : this(null)
    {
    }

    public string RepositoryPath => repositoryPath;

    public virtual int ApplicationCount => DataGenerator.DefaultApplicationCount;

    public virtual int TablePerApplicationCount => DataGenerator.DefaultTablePerApplicationCount;

    public virtual int FieldPerTableCount => DataGenerator.DefaultTablePerApplicationCount;

    public virtual int ConstantPerTableCount => DataGenerator.DefaultTablePerApplicationCount;

    public virtual int ResourcePerTableCount => DataGenerator.DefaultTablePerApplicationCount;

    public async Task CustomizeAsync(IFixture fixture)
    {
        var serviceProvider = fixture.Create<IServiceProvider>();
        var model = serviceProvider.GetRequiredService<IDataModel>();
        var connection = CreateConnection(fixture, serviceProvider, model);
        var tip = await connection.Repository.GetCommittishAsync("main");

        fixture.Register(() => connection);
        fixture.Register<IConnection>(() => connection);
        fixture.Register(() => connection.Repository);

        fixture.LazyRegister(() => connection.GetApplications(tip).Last());
        fixture.LazyRegister(() => connection.GetTables(tip, fixture.Create<Application>()).Last());
        fixture.LazyRegister(() => connection.GetFields(tip, fixture.Create<Table>()).Last());
        fixture.LazyRegister(() => connection.GetConstants(tip, fixture.Create<Table>()).Last());
        fixture.LazyRegister(() => connection.GetResourcesAsync(tip, fixture.Create<Table>()).ToEnumerable().OrderBy(r => r.Path).Last());
    }

    private IConnectionInternal CreateConnection(IFixture fixture, IServiceProvider serviceProvider, IDataModel model)
    {
        var repositoryFactory = serviceProvider.GetRequiredService<ConnectionFactory>();
        var path = RepositoryPath;
        if (path == null)
        {
            path = GitObjectDbFixture.GetAvailableFolderPath();
            var template = GetTemplatePath(fixture, fixture.Create<IServiceProvider>(), fixture.Create<IDataModel>());
            var alreadyExists = GitConnection.IsValid(path);
            if (!alreadyExists)
            {
                DirectoryUtils.CopyFilesRecursively(template, path);
            }
        }
        return (IConnectionInternal)repositoryFactory(path, model);
    }

    private string GetTemplatePath(IFixture fixture, IServiceProvider serviceProvider, IDataModel model)
    {
        var serializer = serviceProvider.GetRequiredService<INodeSerializer>();
        lock (_templates)
        {
            return _templates.GetOrAdd($"{GetType().Name}_{serializer.FileExtension}", CreateTemplate);
        }
        string CreateTemplate(string key)
        {
            var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "Templates", key);
            DirectoryUtils.Delete(path, false);

            var repositoryFactory = serviceProvider.GetRequiredService<ConnectionFactory>();
            using var repository = (IConnectionInternal)repositoryFactory(path, model);
            var software = new DataGenerator(repository, ApplicationCount, TablePerApplicationCount, FieldPerTableCount, ConstantPerTableCount, ResourcePerTableCount);
            AsyncHelper.RunSync(() => software.CreateDataAsync("Commit message", fixture.Create<Signature>()));
            return path;
        }
    }
}
