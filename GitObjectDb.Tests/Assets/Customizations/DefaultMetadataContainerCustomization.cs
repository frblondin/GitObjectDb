using Autofac;
using Autofac.Extensions.DependencyInjection;
using AutoFixture;
using AutoFixture.Kernel;
using GitObjectDb.Models;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace GitObjectDb.Tests.Assets.Customizations
{
    public class DefaultMetadataContainerCustomization : ICustomization, ISpecimenBuilder
    {
        readonly IContainer _container;

        public DefaultMetadataContainerCustomization()
        {
            IServiceProvider serviceProvider = null;

            var builder = new ContainerBuilder();
            builder.RegisterAssemblyModules(typeof(GitObjectDb.Autofac.AutofacModule).Assembly);
            builder.RegisterAssemblyModules(Assembly.GetExecutingAssembly());
            builder.Register(_ => serviceProvider);
            _container = builder.Build();

            // Update captured variable returned by container
            serviceProvider = new AutofacServiceProvider(_container);
        }

        public void Customize(IFixture fixture)
        {
            fixture.Customizations.Add(this);
        }

        public object Create(object request, ISpecimenContext context) =>
            (request is Type t ? _container.ResolveOptional(t) : null) ??
            new NoSpecimen();
    }
}
