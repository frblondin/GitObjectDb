using GitObjectDb.Compare;
using GitObjectDb.Migrations;
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
    public class ObjectRepository : AbstractObjectRepository
    {
        public ObjectRepository(IServiceProvider serviceProvider, IObjectRepositoryContainer container, Guid id, string name, Version version, IImmutableList<RepositoryDependency> dependencies, ILazyChildren<IMigration> migrations, ILazyChildren<Application> applications)
            : base(serviceProvider, container, id, name, version, dependencies, migrations)
        {
            Applications = (applications ?? throw new ArgumentNullException(nameof(applications))).AttachToParent(this);
        }

        public ILazyChildren<Application> Applications { get; }
    }
}
