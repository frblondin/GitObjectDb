using AutoFixture;
using AutoFixture.Kernel;
using GitObjectDb.Tests.Assets.Loggers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Models.Software;
using System;
using YamlDotNet.Serialization.NamingConventions;

namespace GitObjectDb.Tests.Assets;

public class DefaultServiceProviderCustomization : ICustomization, ISpecimenBuilder
{
    readonly IServiceProvider _serviceProvider;

    public DefaultServiceProviderCustomization()
        : this(false)
    {
    }

    public DefaultServiceProviderCustomization(bool useYaml)
    {
        var collection = new ServiceCollection().AddSoftwareModel();
        if (useYaml)
        {
            collection.AddGitObjectDb(c => c.AddYamlDotNet(CamelCaseNamingConvention.Instance));
        }
        else
        {
            collection.AddGitObjectDb(c => c.AddSystemTextJson());
        }
        _serviceProvider = collection
            .AddLogging(builder =>
                builder.SetMinimumLevel(LogLevel.Trace)
                        .AddProvider(new ConsoleProvider()))
            .BuildServiceProvider();
    }

    public void Customize(IFixture fixture)
    {
        fixture.Register(UniqueId.CreateNew);
        fixture.Customizations.Add(this);
    }

    public object Create(object request, ISpecimenContext context) =>
        (request is Type t ? _serviceProvider.GetService(t) : null) ??
        new NoSpecimen();
}
