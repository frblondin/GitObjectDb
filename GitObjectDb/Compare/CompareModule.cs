using System;
using System.Collections.Generic;
using System.Text;
using Autofac;

namespace GitObjectDb.Compare
{
    public class CompareModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ComputeTreeChanges>();
        }
    }
}
