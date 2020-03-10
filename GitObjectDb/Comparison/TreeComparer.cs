using GitObjectDb.Serialization.Json;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace GitObjectDb.Comparison
{
    internal static class TreeComparer
    {
        internal static NodeChanges Compare(Repository repository, Tree oldTree, Tree newTree = null)
        {
            using (var changes = repository.Diff.Compare<TreeChanges>(oldTree, newTree ?? repository.Head.Tip.Tree))
            {
                var modified = from change in changes.Modified
                               let oldEntry = repository.Lookup<Blob>(change.OldOid)
                               let old = DefaultSerializer.Deserialize(oldEntry.GetContentStream(), change.OldPath).Node
                               let newEntry = repository.Lookup<Blob>(change.Oid)
                               let @new = DefaultSerializer.Deserialize(newEntry.GetContentStream(), change.Path).Node
                               let differences = NodeComparer.Compare(old, @new)
                               where differences.Differences.Any()
                               select new NodeChange(old, @new, NodeChangeStatus.Edit, differences);
                var added = from change in changes.Added
                            let newEntry = repository.Lookup<Blob>(change.Oid)
                            let @new = DefaultSerializer.Deserialize(newEntry.GetContentStream(), change.Path).Node
                            select new NodeChange(null, @new, NodeChangeStatus.Add);
                var deleted = from change in changes.Deleted
                              let oldEntry = repository.Lookup<Blob>(change.OldOid)
                              let old = DefaultSerializer.Deserialize(oldEntry.GetContentStream(), change.OldPath).Node
                              select new NodeChange(old, null, NodeChangeStatus.Delete);
                return new NodeChanges(modified.Concat(added).Concat(deleted));
            }
        }
    }
}
