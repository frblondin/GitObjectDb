using GitObjectDb.Attributes;
using GitObjectDb.Models;
using GitObjectDb.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace GitObjectDb.Tests.Assets.Models
{
    [DataContract]
    public class Application : AbstractModel
    {
        new public Instance Instance => (Instance)base.Instance;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public override IEnumerable<IMetadataObject> Children => Pages;

        readonly LazyChildren<Page> _pages;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public IImmutableList<Page> Pages => _pages[this];

        public Application(Guid id, string name, LazyChildren<Page> pages) : base(id, name)
        {
            _pages = pages ?? throw new ArgumentNullException(nameof(pages));
        }

        protected override void CreateNewParent(IMetadataObject @new) => Instance.CreateNew(this, (Application)@new);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override IMetadataObject CloneSubTree(Expression predicate = null)
        {
            var reflector = new PredicateReflector<Application>((Expression<Predicate<Application>>)predicate);
            return new Application(Id,
                reflector.ProcessArgument(nameof(Name), Name),
                _pages.Clone(update: p => (Page)p.CloneSubTree()));
        }

        internal void CreateNew(Page old, Page @new)
        {
            var result = new Application(Id,
                Name,
                _pages.Clone(old, @new, p => (Page)p.CloneSubTree()));
            ((IMetadataObject)@new).AttachToParent(result);
            Instance.CreateNew(this, result);
        }
    }
}
