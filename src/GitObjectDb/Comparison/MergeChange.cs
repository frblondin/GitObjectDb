using Fasterflect;
using GitObjectDb.Internal.Commands;
using LibGit2Sharp;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace GitObjectDb.Comparison
{
    /// <summary>Contains all details about an item merge.</summary>
    [DebuggerDisplay("Status = {Status}, Path = {Path}")]
    public sealed class MergeChange
    {
        internal MergeChange(ComparisonPolicy policy)
        {
            Policy = policy ?? ComparisonPolicy.Default;
        }

        /// <summary>Gets the ancestor.</summary>
        public ITreeItem? Ancestor { get; internal set; }

        /// <summary>Gets the node on our side.</summary>
        public ITreeItem? Ours { get; internal set; }

        /// <summary>Gets the node on their side.</summary>
        public ITreeItem? Theirs { get; internal set; }

        /// <summary>Gets the parent root deleted node on our side.</summary>
        public ITreeItem? OurRootDeletedParent { get; internal set; }

        /// <summary>Gets the parent root deleted node on their side.</summary>
        public ITreeItem? TheirRootDeletedParent { get; internal set; }

        /// <summary>Gets the merged node.</summary>
        public ITreeItem? Merged { get; private set; }

        /// <summary>Gets the node path.</summary>
        public DataPath Path => (Theirs ?? Ours ?? Ancestor)?.Path ?? throw new InvalidOperationException();

        /// <summary>Gets the list of conflicts.</summary>
        public IImmutableList<MergeValueConflict> Conflicts { get; internal set; } = ImmutableList.Create<MergeValueConflict>();

        /// <summary>Gets the merge policy.</summary>
        public ComparisonPolicy Policy { get; private set; }

        /// <summary>Gets the merge status.</summary>
        public ItemMergeStatus Status { get; internal set; }

        internal MergeChange Initialize()
        {
            var type = CreateMergeInstance();

            var conflicts = ImmutableList.CreateBuilder<MergeValueConflict>();
            foreach (var property in Comparer.GetProperties(type, Policy))
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

        private Type CreateMergeInstance()
        {
            var nonNull = Ours ?? Theirs ?? Ancestor ?? throw new NullReferenceException();
            var type = nonNull.GetType();
            var path = nonNull.Path ?? throw new NullReferenceException();
            if (nonNull is Node node)
            {
                Merged = (ITreeItem)Reflect.Constructor(type, typeof(UniqueId)).Invoke(node.Id);
                ((ITreeItemInternal)Merged).Path = path;
            }
            else if (nonNull is Resource resource)
            {
                Merged = new Resource(resource.Path, Array.Empty<byte>());
            }
            else
            {
                throw new NotImplementedException();
            }
            return type;
        }

        internal void Transform(UpdateTreeCommand update, ObjectDatabase database, TreeDefinition tree, Tree? reference)
        {
            switch (Status)
            {
                case ItemMergeStatus.Add:
                case ItemMergeStatus.Edit:
                    if (Merged is null)
                    {
                        throw new InvalidOperationException("No merge value has been set.");
                    }
                    update.CreateOrUpdate(Merged).Invoke(database, tree, reference);
                    break;
                case ItemMergeStatus.Delete:
                    if (Ancestor is null)
                    {
                        throw new InvalidOperationException("The deletion change does not contain any ancestor.");
                    }
                    UpdateTreeCommand.Delete(Ancestor).Invoke(database, tree, reference);
                    break;
                default:
                    throw new GitObjectDbException("Remaining conflicts.");
            }
        }

        private void UpdateStatus()
        {
            if (OurRootDeletedParent != null || TheirRootDeletedParent != null)
            {
                Status = ItemMergeStatus.TreeConflict;
            }
            else if (Ancestor != null && (Theirs == null || Ours == null))
            {
                // Deletion
                var nonNull = Theirs ?? Ours;
                Status = nonNull != null && !Comparer.Compare(Ancestor, nonNull, Policy).AreEqual ?
                    ItemMergeStatus.TreeConflict :
                    ItemMergeStatus.Delete;
            }
            else
            {
                if (Conflicts.Any(c => !c.IsResolved))
                {
                    Status = ItemMergeStatus.EditConflict;
                }
                else if (Comparer.Compare(Ours, Merged, Policy).AreEqual)
                {
                    Status = ItemMergeStatus.NoChange;
                }
                else
                {
                    Status = Ancestor == null ? ItemMergeStatus.Add : ItemMergeStatus.Edit;
                }
            }
        }

        private bool TryMergePropertyValue(PropertyInfo property, out MemberSetter setter, out object? ancestorValue, out object? ourValue, out object? theirValue)
        {
            var getter = Reflect.PropertyGetter(property);
            setter = Reflect.PropertySetter(property);
            ancestorValue = Ancestor != null ? getter(Ancestor) : null;
            ourValue = Ours != null ? getter(Ours) : null;
            theirValue = Theirs != null ? getter(Theirs) : null;

            if (Ours != null && Theirs != null)
            {
                // Both values are equal -> no conflict
                if (Comparer.Compare(ourValue, theirValue, Policy).AreEqual)
                {
                    setter(Merged, ourValue);
                    return true;
                }

                // Only changed in their changes -> no conflict
                if (Ancestor != null && Comparer.Compare(ancestorValue, ourValue, Policy).AreEqual)
                {
                    setter(Merged, theirValue);
                    return true;
                }

                // Only changed in our changes -> no conflict
                if (Ancestor != null && Comparer.Compare(ancestorValue, theirValue, Policy).AreEqual)
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
