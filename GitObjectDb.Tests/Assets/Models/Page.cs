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
    public class Page : AbstractModel
    {
        public Application Application => (Application)Parent ?? throw new NullReferenceException("No parent has been set.");

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public override IEnumerable<IMetadataObject> Children => Fields;

        readonly LazyChildren<Field> _fields;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public IImmutableList<Field> Fields => _fields[this];

        public Page(Guid id, string name, LazyChildren<Field> fields) : base(id, name)
        {
            _fields = fields ?? throw new ArgumentNullException(nameof(fields));
        }

        protected override void CreateNewParent(IMetadataObject @new) => Application.CreateNew(this, (Page)@new);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override IMetadataObject CloneSubTree(Expression predicate = null)
        {
            var reflector = new PredicateReflector<Page>((Expression<Predicate<Page>>)predicate);
            return new Page(Id,
                reflector.ProcessArgument(nameof(Name), Name),
                _fields.Clone(update: f => (Field)f.CloneSubTree()));
        }

        internal void CreateNew(Field old, Field @new)
        {
            var result = new Page(Id,
                Name,
                _fields.Clone(old, @new, f => (Field)f.CloneSubTree()));
            ((IMetadataObject)@new).AttachToParent(result);
            Application.CreateNew(this, result);
        }
    }
}
