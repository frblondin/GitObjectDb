using AutoFixture;
using AutoFixture.Kernel;
using GitObjectDb.Tests.Assets.Loggers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Models.Software;
using System;

namespace GitObjectDb.Tests.Assets
{
    public class DefaultServiceProviderCustomization : ICustomization, ISpecimenBuilder
    {
        readonly IServiceProvider _serviceProvider;

        public DefaultServiceProviderCustomization()
        {
            _serviceProvider = new ServiceCollection()
                .AddGitObjectDb()
                .AddSoftwareModel()
                .AddLogging(builder =>
                    builder.SetMinimumLevel(LogLevel.Trace)
                           .AddProvider(new ConsoleProvider()))
                .BuildServiceProvider();
        }

        public void Customize(IFixture fixture)
        {
            fixture.Customizations.Add(this);
        }

        public object Create(object request, ISpecimenContext context) =>
            (request is Type t ? _serviceProvider.GetService(t) : null) ??
            new NoSpecimen();
    }
}
