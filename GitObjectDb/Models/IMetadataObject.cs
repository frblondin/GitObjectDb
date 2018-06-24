using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace GitObjectDb.Models
{
    public interface IMetadataObject
    {
        IInstance Instance { get; }
        IMetadataObject Parent { get; }

        Guid Id { get; }
        string Name { get; }
        IEnumerable<IMetadataObject> Children { get; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        void AttachToParent(IMetadataObject parent);
    }
}
