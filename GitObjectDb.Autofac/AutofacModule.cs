using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Autofac.Core.Activators.Delegate;
using Autofac.Core.Lifetime;
using GitObjectDb.Compare;
using GitObjectDb.Reflection;

namespace GitObjectDb.Autofac
{
    public class AutofacModule : Module
    {
        const string HandlerKeyName = "handler";

        protected override void Load(ContainerBuilder builder)
        {
            RegisterComponents(builder);
            RegisterDecorators(builder);
        }

        static void RegisterComponents(ContainerBuilder builder)
        {
            foreach (var implementation in DefaultInterfaceMapping.Implementations)
            {
                var registration = builder.RegisterType(implementation.Type);
                if (implementation.InterfaceType != null)
                {
                    registration.As(implementation.InterfaceType);
                    RegisterAsHandlerIfOneDecoratorExists(implementation.InterfaceType, registration);
                }
            }
        }

        static void RegisterAsHandlerIfOneDecoratorExists(Type @interface, IRegistrationBuilder<object, ConcreteReflectionActivatorData, SingleRegistrationStyle> registration)
        {
            if (DefaultInterfaceMapping.Decorators.Any(d => d.InterfaceType == @interface))
            {
                registration.Named(HandlerKeyName, @interface);
            }
        }

        static void RegisterDecorators(ContainerBuilder builder)
        {
            foreach (var decorator in DefaultInterfaceMapping.Decorators)
            {
                // Activator calls the adapter and passes the inner component
                object Activate(IComponentContext context, IEnumerable<Parameter> _) =>
                    decorator.Adapter(context.ResolveKeyed(HandlerKeyName, decorator.InterfaceType));

                var registration = RegistrationBuilder.CreateRegistration(
                    Guid.NewGuid(),
                    new RegistrationData(new KeyedService(HandlerKeyName, decorator.InterfaceType))
                    {
                        Sharing = decorator.SingleInstance ? InstanceSharing.Shared : InstanceSharing.None,
                        Lifetime = decorator.SingleInstance ? (IComponentLifetime)new RootScopeLifetime() : new CurrentScopeLifetime()
                    },
                    new DelegateActivator(decorator.InterfaceType, Activate),
                    new[] { new TypedService(decorator.InterfaceType) });
                builder.RegisterComponent(registration);
            }
        }
    }
}
