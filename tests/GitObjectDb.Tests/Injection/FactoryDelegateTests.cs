using AutoFixture.NUnit3;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace GitObjectDb.Tests.Injection
{
    public class FactoryDelegateTests
    {
        [Test]
        [AutoData]
        public void FactoryDelegateInstantiatesImplementation(ServiceCollection services, string value)
        {
            // Act
            var provider = services.AddFactoryDelegate<Factory, Implementation>().BuildServiceProvider();
            var instance = provider.GetRequiredService<Factory>().Invoke(value);

            // Assert
            Assert.That(instance.Param, Is.EqualTo(value));
        }

        [Test]
        [AutoData]
        public void FactoryDelegateFailsWhenNoDecoratedConstructor(ServiceCollection services)
        {
            // Assert
            Assert.Throws<InvalidOperationException>(() => services.AddFactoryDelegate<Factory, MissingDecorationImplementation>());
        }

        [Test]
        [AutoData]
        public void FactoryDelegateFailsWhenMultipleDecorationImplementation(ServiceCollection services)
        {
            // Assert
            Assert.Throws<InvalidOperationException>(() => services.AddFactoryDelegate<Factory, MultipleDecorationImplementation>());
        }

        [Test]
        [AutoData]
        public void FactoryDelegateFailsWhenNoMatchingConstructorParamTypeImplementation(ServiceCollection services)
        {
            // Assert
            Assert.Throws<InvalidOperationException>(() => services.AddFactoryDelegate<Factory, NoMatchingConstructorParamTypeImplementation>());
        }

#pragma warning disable SA1201 // Elements must appear in the correct order
#pragma warning disable S1144 // Unused private types or members should be removed
        delegate IInterface Factory(string param);

        interface IInterface
        {
            IServiceProvider ServiceProvider { get; }

            string Param { get; }
        }

        class Implementation : IInterface
        {
            [ActivatorUtilitiesConstructor]
            public Implementation(IServiceProvider serviceProvider, string param)
            {
                ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
                Param = param ?? throw new ArgumentNullException(nameof(param));
            }

            public IServiceProvider ServiceProvider { get; }

            public string Param { get; }
        }

        class MissingDecorationImplementation : Implementation
        {
            public MissingDecorationImplementation(IServiceProvider serviceProvider, string param)
                : base(serviceProvider, param)
            {
            }
        }

        class MultipleDecorationImplementation : Implementation
        {
            [ActivatorUtilitiesConstructor]
            public MultipleDecorationImplementation(IServiceProvider serviceProvider, string param)
                : base(serviceProvider, param)
            {
            }

            [ActivatorUtilitiesConstructor]
            public MultipleDecorationImplementation(IServiceProvider serviceProvider, int param)
                : base(serviceProvider, param.ToString(CultureInfo.InvariantCulture))
            {
            }
        }

        class NoMatchingConstructorParamTypeImplementation : Implementation
        {
            [ActivatorUtilitiesConstructor]
            public NoMatchingConstructorParamTypeImplementation(IServiceProvider serviceProvider, int param)
                : base(serviceProvider, param.ToString(CultureInfo.InvariantCulture))
            {
            }
        }
#pragma warning restore S1144 // Unused private types or members should be removed
#pragma warning restore SA1201 // Elements must appear in the correct order
    }
}
