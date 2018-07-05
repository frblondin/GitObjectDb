using AutoFixture;
using AutoFixture.Kernel;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace GitObjectDb.Tests.Assets.Customizations
{
    public class DefaultMetadataContainerCustomization : ICustomization, ISpecimenBuilder
    {
        readonly IServiceProvider _serviceProvider;

        public DefaultMetadataContainerCustomization()
        {
            var services = new ServiceCollection();
            services.AddGitObjectDb();
            _serviceProvider = services.BuildServiceProvider();
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
