using AutoFixture;
using AutoFixture.Kernel;
using GitObjectDb.Tests.Assets.Loggers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace GitObjectDb.Tests.Assets
{
    public class DefaultContainerCustomization : ICustomization, ISpecimenBuilder
    {
        readonly IServiceProvider _serviceProvider;

        public DefaultContainerCustomization()
        {
            _serviceProvider = new ServiceCollection()
                .AddGitObjectDb()
                .AddLogging(builder =>
                    builder.SetMinimumLevel(LogLevel.Trace)
                           .AddProvider(new ConsoleProvider())
                )
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
