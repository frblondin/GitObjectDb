using Autofac;
using GitObjectDb.Compare;
using GitObjectDb.Models;
using GitObjectDb.Reflection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;

namespace GitObjectDb.Tests.Assets.Models
{
    public class Instance : AbstractInstance
    {
        public ILazyChildren<Application> Applications { get; }

        public delegate Instance Factory(Guid id, string name, LazyChildren<Application> applications);
        public Instance(IServiceProvider serviceProvider, Guid id, string name, ILazyChildren<Application> applications) :
            base(serviceProvider, id, name)
        {
            Applications = (applications ?? throw new ArgumentNullException(nameof(applications))).AttachToParent(this);
        }
    }
}
