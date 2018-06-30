using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace GitObjectDb.Models
{
    public interface ILazyChildren<TChild> : ILazyChildren, IReadOnlyList<TChild>
        where TChild : class, IMetadataObject
    {
        [EditorBrowsable(EditorBrowsableState.Never)] ILazyChildren<TChild> AttachToParent(IMetadataObject parent);
    }

    public interface ILazyChildren : IEnumerable
    {
        IMetadataObject Parent { get; }
        bool AreChildrenLoaded { get; }
        bool ForceVisit { get; }
        ILazyChildren Clone(Func<IMetadataObject, IMetadataObject> update, bool forceVisit);
    }
}
