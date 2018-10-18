using AutoFixture;
using AutoFixture.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace GitObjectDb.Tests.Assets.Customizations
{
    public class NSubstituteForAbstractTypesCustomization : ICustomization, ISpecimenBuilder
    {
        public bool ExcludeEnumerableTypes { get; set; } = true;

        public void Customize(IFixture fixture)
        {
            fixture.Customizations.Add(this);
        }

        public object Create(object request, ISpecimenContext context)
        {
            if (request is Type t && (t.IsInterface || t.IsAbstract) && !ShouldExclude(t))
            {
                return Substitute.For(new[] { t }, Array.Empty<Type>());
            }

            return new NoSpecimen();
        }

        bool ShouldExclude(Type type)
        {
            if (ExcludeEnumerableTypes)
            {
                var interfaces = type.GetInterfaces().Concat(Enumerable.Repeat(type, 1));
                return interfaces.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            }
            return false;
        }
    }
}
