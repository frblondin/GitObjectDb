using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Autofac.Core.Activators.Delegate;
using Autofac.Core.Lifetime;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GitObjectDb.Autofac
{
    /// <summary>
    /// Module providing all types necessary to run GitObjectDb using Autofac.
    /// </summary>
    /// <seealso cref="Module" />
    public class AutofacModule : Module
    {
        const string HandlerKeyName = "handler";

        /// <inheritdoc />
        protected override void Load(ContainerBuilder builder)
        {
            RegisterComponents(builder);
            RegisterDecorators(builder);
        }

        static void RegisterComponents(ContainerBuilder builder)
        {
            foreach (var (type, interfaceType) in DefaultServiceMapping.Implementations)
            {
                var registration = builder.RegisterType(type);
                if (interfaceType != null)
                {
                    registration.As(interfaceType);
                    RegisterAsHandlerIfOneDecoratorExists(interfaceType, registration);
                }
            }
        }

        static void RegisterAsHandlerIfOneDecoratorExists(Type @interface, IRegistrationBuilder<object, ConcreteReflectionActivatorData, SingleRegistrationStyle> registration)
        {
            if (DefaultServiceMapping.Decorators.Any(d => d.InterfaceType == @interface))
            {
                registration.Named(HandlerKeyName, @interface);
            }
        }

        static void RegisterDecorators(ContainerBuilder builder)
        {
            foreach (var (interfaceType, adapter, singleInstance) in DefaultServiceMapping.Decorators)
            {
                // Activator calls the adapter and passes the inner component
                object Activate(IComponentContext context, IEnumerable<Parameter> parameters) =>
                    adapter(context.ResolveKeyed(HandlerKeyName, interfaceType));

                var registration = RegistrationBuilder.CreateRegistration(
                    Guid.NewGuid(),
                    new RegistrationData(new KeyedService(HandlerKeyName, interfaceType))
                    {
                        Sharing = singleInstance ? InstanceSharing.Shared : InstanceSharing.None,
                        Lifetime = singleInstance ? (IComponentLifetime)new RootScopeLifetime() : new CurrentScopeLifetime()
                    },
                    new DelegateActivator(interfaceType, Activate),
                    new[] { new TypedService(interfaceType) });
                builder.RegisterComponent(registration);
            }
        }
    }
}
