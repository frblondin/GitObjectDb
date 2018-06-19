using Autofac;
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
            var builder = new ContainerBuilder();
            builder.RegisterAssemblyModules(typeof(IMetadataObject).Assembly);
            builder.RegisterAssemblyModules(Assembly.GetExecutingAssembly());
            _container = builder.Build();
        }

        public void Customize(IFixture fixture)
        {
            fixture.Customizations.Add(this);
        }

        public object Create(object request, ISpecimenContext context)
        {
            if (request is Type t)
            {
                var result = _container.ResolveOptional(t);
                return result ?? new NoSpecimen();
            }
            return new NoSpecimen();
        }
    }
}
