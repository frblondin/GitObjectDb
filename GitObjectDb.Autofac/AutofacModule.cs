using System;
using Autofac;
using GitObjectDb.Compare;
using GitObjectDb.Utils;

namespace GitObjectDb.Autofac
{
    public class AutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            RegisterModelDataAccessorProvider(builder);
            RegisterComputeTreeChanges(builder);
        }

        static void RegisterModelDataAccessorProvider(ContainerBuilder builder)
        {
            builder.RegisterType<ModelDataAccessorProvider>().Named<IModelDataAccessorProvider>("handler");
            builder.RegisterDecorator<IModelDataAccessorProvider>(
                inner => new CachedModelDataAccessorProvider(inner),
                fromKey: "handler");
        }

        static void RegisterComputeTreeChanges(ContainerBuilder builder)
        {
            builder.RegisterType<ComputeTreeChanges>();
        }
    }
}
