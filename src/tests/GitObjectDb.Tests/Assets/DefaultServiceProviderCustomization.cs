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
    private readonly bool _useYaml;

    public DefaultServiceProviderCustomization()
        : this(false)
    {
    }

    public DefaultServiceProviderCustomization(bool useYaml)
    {
        _useYaml = useYaml;
    }

    public void Customize(IFixture fixture)
    {
        fixture.Register(UniqueId.CreateNew);
        fixture.Freeze<IServiceCollection>(c => c
            .FromFactory(() => new ServiceCollection())
            .Do(AddServices));
        IServiceProvider singleton = null;
        fixture.Register(() => singleton ??= fixture.Create<IServiceCollection>().BuildServiceProvider());
        fixture.Customizations.Add(this);
    }

    private void AddServices(IServiceCollection services)
    {
        services
            .AddLogging(builder =>
                builder.SetMinimumLevel(LogLevel.Trace)
                        .AddProvider(new ConsoleProvider()))
            .AddMemoryCache()
            .AddSoftwareModel()
            .AddGitObjectDb();
        if (_useYaml)
        {
            services.AddGitObjectDbYamlDotNet(CamelCaseNamingConvention.Instance);
        }
        else
        {
            services.AddGitObjectDbSystemTextJson();
        }
    }

    public object Create(object request, ISpecimenContext context) =>
        (request is Type t ? context.Create<IServiceProvider>().GetService(t) : null) ??
        new NoSpecimen();
}
