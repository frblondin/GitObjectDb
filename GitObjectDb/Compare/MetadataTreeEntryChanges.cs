using GitObjectDb.Models;
using System;
using System.Diagnostics;

namespace GitObjectDb.Compare
{
    [DebuggerDisplay("Old = {Old.Id}, New = {New.Id}")]
    public class MetadataTreeEntryChanges
    {
        public IMetadataObject Old { get; }
        public IMetadataObject New { get; }

        public MetadataTreeEntryChanges(IMetadataObject old, IMetadataObject @new)
        {
            if (old == null && @new == null) throw new ArgumentNullException($"{nameof(old)} and {nameof(@new)}");

            Old = old;
            New = @new;
        }
    }
}
