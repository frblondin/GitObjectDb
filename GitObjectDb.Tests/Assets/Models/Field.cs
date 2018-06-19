using GitObjectDb.Attributes;
using GitObjectDb.Models;
using GitObjectDb.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace GitObjectDb.Tests.Assets.Models
{
    [DataContract]
    public class Field : AbstractModel
    {
        public override IEnumerable<IMetadataObject> Children => Enumerable.Empty<IMetadataObject>();

        public Page Page => (Page)Parent ?? throw new NullReferenceException("No parent has been set.");


        public Field(Guid id, string name) : base(id, name)
        {
        }

        protected override void CreateNewParent(IMetadataObject @new) => Page.CreateNew(this, (Field)@new);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override IMetadataObject CloneSubTree(Expression predicate = null)
        {
            var reflector = new PredicateReflector<Field>((Expression<Predicate<Field>>)predicate);
            return new Field(Id,
                reflector.ProcessArgument(nameof(Name), Name));
        }
    }
}
