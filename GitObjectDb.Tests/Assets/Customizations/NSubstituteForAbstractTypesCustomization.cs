using AutoFixture;
using AutoFixture.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace GitObjectDb.Tests.Assets.Customizations
{
    public class NSubstituteForAbstractTypesCustomization : ICustomization, ISpecimenBuilder
    {
        public void Customize(IFixture fixture)
        {
            fixture.Customizations.Add(this);
        }

        public object Create(object request, ISpecimenContext context)
        {
            if (request is Type t && (t.IsInterface || t.IsAbstract))
            {
                return Substitute.For(new[] { t }, Array.Empty<Type>());
            }

            return new NoSpecimen();
        }
    }
}
