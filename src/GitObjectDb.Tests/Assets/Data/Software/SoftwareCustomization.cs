using AutoFixture;
using GitObjectDb.Model;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using Models.Software;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace GitObjectDb.Tests.Assets.Data.Software;

public class SoftwareCustomization : ICustomization
{
    public SoftwareCustomization()
        : this(DataGenerator.DefaultApplicationCount, DataGenerator.DefaultTablePerApplicationCount, DataGenerator.DefaultFieldPerTableCount, DataGenerator.DefaultConstantPerTableCount, DataGenerator.DefaultResourcePerTableCount)
    {
    }

    public SoftwareCustomization(string repositoryPath)
        : this(DataGenerator.DefaultApplicationCount, DataGenerator.DefaultTablePerApplicationCount, DataGenerator.DefaultFieldPerTableCount, DataGenerator.DefaultConstantPerTableCount, DataGenerator.DefaultResourcePerTableCount, repositoryPath)
    {
    }

    public SoftwareCustomization(int applicationCount, int tablePerApplicationCount, int fieldPerTableCount, int constantPerTableCount, int resourcePerTableCount, string repositoryPath = null)
    {
        ApplicationCount = applicationCount;
        TablePerApplicationCount = tablePerApplicationCount;
        FieldPerTableCount = fieldPerTableCount;
        ConstantPerTableCount = constantPerTableCount;
        ResourcePerTableCount = resourcePerTableCount;
        RepositoryPath = repositoryPath;
    }

    public string RepositoryPath { get; }

    public int ApplicationCount { get; }

    public int TablePerApplicationCount { get; }

    public int FieldPerTableCount { get; }

    public int ConstantPerTableCount { get; }

    public int ResourcePerTableCount { get; }

    public void Customize(IFixture fixture)
    {
        var serviceProvider = fixture.Create<IServiceProvider>();

        var model = serviceProvider.GetRequiredService<IDataModel>();
        var connection = new Lazy<IConnectionInternal>(() => CreateConnection(fixture, serviceProvider, model));

        fixture.Inject(model);
        fixture.Register(() => connection.Value);
        fixture.Register<IConnection>(() => connection.Value);
        fixture.Register(() => connection.Value.Repository);

        fixture.LazyRegister(() => connection.Value.GetApplications().Last());
        fixture.LazyRegister(() => connection.Value.GetTables(fixture.Create<Application>()).Last());
        fixture.LazyRegister(() => connection.Value.GetFields(fixture.Create<Table>()).Last());
        fixture.LazyRegister(() => connection.Value.GetConstants(fixture.Create<Table>()).Last());
        fixture.LazyRegister(() => connection.Value.GetResources(fixture.Create<Table>()).Last());
    }

    private IConnectionInternal CreateConnection(IFixture fixture, IServiceProvider serviceProvider, IDataModel model)
    {
        var path = RepositoryPath ?? GitObjectDbFixture.GetAvailableFolderPath();
        var alreadyExists = Directory.Exists(path);
        var repositoryFactory = serviceProvider.GetRequiredService<ConnectionFactory>();
        var result = (IConnectionInternal)repositoryFactory(path, model);
        if (!alreadyExists)
        {
            var software = new DataGenerator(result, ApplicationCount, TablePerApplicationCount, FieldPerTableCount, ConstantPerTableCount, ResourcePerTableCount);
            software.CreateData(fixture.Create<string>(), fixture.Create<Signature>());
        }
        return result;
    }
}
