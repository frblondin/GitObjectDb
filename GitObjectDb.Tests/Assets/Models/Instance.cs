using Autofac;
using GitObjectDb.Compare;
using GitObjectDb.Models;
using GitObjectDb.Utils;
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
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public override IEnumerable<IMetadataObject> Children => Applications;

        readonly LazyChildren<Application> _applications;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public IImmutableList<Application> Applications => _applications[this];

        protected Factory _factory { get; }

        public delegate Instance Factory(Guid id, string name, LazyChildren<Application> applications);
        public Instance(IServiceProvider serviceProvider, IModelDataAccessorProvider dataAccessorProvider, ComputeTreeChanges.Factory computeTreeChangesFactory, Guid id, string name, LazyChildren<Application> applications) :
            base(serviceProvider, dataAccessorProvider, computeTreeChangesFactory, id, name)
        {
            _factory = (Factory)serviceProvider.GetService(typeof(Factory)) ?? throw new ArgumentNullException(nameof(serviceProvider));
            _applications = applications ?? throw new ArgumentNullException(nameof(applications));
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override IMetadataObject CloneSubTree(Expression predicate = null)
        {
            var reflector = new PredicateReflector<Instance>((Expression<Predicate<Instance>>)predicate);
            return _factory(Id,
                reflector.ProcessArgument(nameof(Name), Name),
                _applications.Clone(update: a => (Application)a.CloneSubTree()));
        }

        internal void CreateNew(Application old, Application @new)
        {
            var result = _factory(
              Id,
              Name,
              _applications.Clone(old, @new, a => (Application)a.CloneSubTree()));
            ((IMetadataObject)@new).AttachToParent(result);
        }
    }
}
