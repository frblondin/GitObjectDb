using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace GitObjectDb.Compare
{
    [DebuggerDisplay("+{Added.Count} ~{Modified.Count} -{Deleted.Count}")]
    public class MetadataTreeChanges
    {
        public IImmutableList<MetadataTreeEntryChanges> Modified { get; }
        public IImmutableList<MetadataTreeEntryChanges> Added { get; }
        public IImmutableList<MetadataTreeEntryChanges> Deleted { get; }

        public MetadataTreeChanges(IImmutableList<MetadataTreeEntryChanges> modified, IImmutableList<MetadataTreeEntryChanges> added, IImmutableList<MetadataTreeEntryChanges> deleted)
        {
            Modified = modified ?? throw new ArgumentNullException(nameof(modified));
            Added = added ?? throw new ArgumentNullException(nameof(added));
            Deleted = deleted ?? throw new ArgumentNullException(nameof(deleted));
        }
    }
}
