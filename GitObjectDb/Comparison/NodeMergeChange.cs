using Fasterflect;
using GitObjectDb.Commands;
using LibGit2Sharp;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace GitObjectDb.Comparison
{
    /// <summary>Contains all details about a node merge.</summary>
    [DebuggerDisplay("Status = {Status}, Path = {Path}")]
    public sealed class NodeMergeChange
    {
        internal NodeMergeChange(NodeMergerPolicy policy)
        {
            Policy = policy ?? NodeMergerPolicy.Default;
        }

        /// <summary>Gets the ancestor.</summary>
        public Node Ancestor { get; internal set; }

        /// <summary>Gets the node on our side.</summary>
        public Node Ours { get; internal set; }

        /// <summary>Gets the node on their side.</summary>
        public Node Theirs { get; internal set; }

        /// <summary>Gets the parent root deleted node on our side.</summary>
        public Node OurRootDeletedParent { get; internal set; }

        /// <summary>Gets the parent root deleted node on their side.</summary>
        public Node TheirRootDeletedParent { get; internal set; }

        /// <summary>Gets the merged node.</summary>
        public Node Merged { get; private set; }

        /// <summary>Gets the node path.</summary>
        public Path Path => (Theirs ?? Ours ?? Ancestor).Path;

        /// <summary>Gets the list of conflicts.</summary>
        public IImmutableList<MergeValueConflict> Conflicts { get; internal set; }

        /// <summary>Gets the merge policy.</summary>
        public NodeMergerPolicy Policy { get; private set; }

        /// <summary>Gets the merge status.</summary>
        public NodeMergeStatus Status { get; internal set; }

        internal NodeMergeChange Initialize()
        {
            var (type, id, path) = GetPrimaryInformation();
            Merged = (Node)Reflect.Constructor(type, typeof(UniqueId)).Invoke(id);
            Merged.Path = path;
            var conflicts = ImmutableList.CreateBuilder<MergeValueConflict>();
            foreach (var property in NodeComparer.GetProperties(type))
            {
                if (!TryMergePropertyValue(property, out var setter, out var ancestorValue, out var ourValue, out var theirValue))
                {
                    Action<object> resolveCallback = value =>
                    {
                        setter(Merged, value);
                        UpdateStatus();
                    };
                    var conflict = new MergeValueConflict(property, ancestorValue, ourValue, theirValue, resolveCallback);
                    conflicts.Add(conflict);
                }
            }
            Conflicts = conflicts.ToImmutable();
            UpdateStatus();
            return this;
        }

        internal void Transform(ObjectDatabase database, TreeDefinition tree)
        {
            if (Status == NodeMergeStatus.EditConflict || Status == NodeMergeStatus.TreeConflict)
            {
                throw new GitObjectDbException("Remaining conflicts.");
            }
            UpdateTreeCommand.CreateOrUpdate(Merged).Invoke(database, tree);
        }

        private void UpdateStatus()
        {
            if (OurRootDeletedParent != null || TheirRootDeletedParent != null)
            {
                Status = NodeMergeStatus.TreeConflict;
            }
            else if (Ancestor == null && (Theirs == null || Ours == null))
            {
                Status = NodeMergeStatus.Add;
            }
            else if (Ancestor != null && (Theirs == null || Ours == null))
            {
                Status = NodeMergeStatus.Delete;
            }
            else
            {
                Status = Conflicts.Any(c => !c.IsResolved) ? NodeMergeStatus.EditConflict : NodeMergeStatus.Edit;
            }
        }

        private (Type, UniqueId, Path) GetPrimaryInformation()
        {
            if (Ancestor != null)
            {
                return (Ancestor.GetType(), Ancestor.Id, Ancestor.Path);
            }
            if (Ours != null)
            {
                return (Ours.GetType(), Ours.Id, Ours.Path);
            }
            if (Theirs != null)
            {
                return (Theirs.GetType(), Theirs.Id, Theirs.Path);
            }

            throw new NotSupportedException();
        }

        private bool TryMergePropertyValue(PropertyInfo property, out MemberSetter setter, out object ancestorValue, out object ourValue, out object theirValue)
        {
            var getter = Reflect.PropertyGetter(property);
            setter = Reflect.PropertySetter(property);
            ancestorValue = Ancestor != null ? getter(Ancestor) : null;
            ourValue = Ours != null ? getter(Ours) : null;
            theirValue = Theirs != null ? getter(Theirs) : null;

            if (Ours != null && Theirs != null)
            {
                // Both values are equal -> no conflict
                if (NodeComparer.Compare(ourValue, theirValue).AreEqual)
                {
                    setter(Merged, ourValue);
                    return true;
                }

                // Only changed in their changes -> no conflict
                if (Ancestor != null && NodeComparer.Compare(ancestorValue, ourValue).AreEqual)
                {
                    setter(Merged, theirValue);
                    return true;
                }

                // Only changed in our changes -> no conflict
                if (Ancestor != null && NodeComparer.Compare(ancestorValue, theirValue).AreEqual)
                {
                    setter(Merged, ourValue);
                    return true;
                }
                return false;
            }
            if (Ours != null)
            {
                setter(Merged, ourValue);
                return true;
            }
            if (Theirs != null)
            {
                setter(Merged, theirValue);
                return true;
            }
            if (Ancestor != null)
            {
                setter(Merged, ancestorValue);
                return true;
            }
            throw new NotImplementedException("Expected execution path.");
        }
    }
}
