using Autofac;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Tests.Assets.Models
{
    public class ModelsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<Instance>();
            builder.RegisterType<Application>();
            builder.RegisterType<Page>();
            builder.RegisterType<Field>();
        }
    }
}
