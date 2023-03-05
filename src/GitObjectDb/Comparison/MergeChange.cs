using Fasterflect;
using GitObjectDb.Internal.Commands;
using GitObjectDb.Tools;
using KellermanSoftware.CompareNetObjects;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace GitObjectDb.Comparison;

/// <summary>Contains all details about an item merge.</summary>
[DebuggerDisplay("Status = {Status}, Path = {Path}")]
public sealed class MergeChange
{
    internal MergeChange(ComparisonPolicy policy,
                         TreeItem? ancestor = null,
                         TreeItem? ours = null,
                         TreeItem? theirs = null,
                         TreeItem? ourRootDeletedParent = null,
                         TreeItem? theirRootDeletedParent = null)
    {
        Policy = policy;
        Ancestor = ancestor;
        Ours = ours;
        Theirs = theirs;
        OurRootDeletedParent = ourRootDeletedParent;
        TheirRootDeletedParent = theirRootDeletedParent;

        Path = GetMergePath();
        var type = CreateMergeInstance();

        var conflicts = ProcessPropertyValues(type);
        Conflicts = conflicts.ToImmutable();
        UpdateStatus();
    }

    /// <summary>Gets the ancestor.</summary>
    public TreeItem? Ancestor { get; }

    /// <summary>Gets the node on our side.</summary>
    public TreeItem? Ours { get; }

    /// <summary>Gets the node on their side.</summary>
    public TreeItem? Theirs { get; }

    private bool StillExists => Ours is not null && Theirs is not null;

    /// <summary>Gets the parent root deleted node on our side.</summary>
    public TreeItem? OurRootDeletedParent { get; }

    /// <summary>Gets the parent root deleted node on their side.</summary>
    public TreeItem? TheirRootDeletedParent { get; }

    private bool AddedOrEdited => (Ancestor is null && (Theirs is not null || Ours is not null)) ||
                                  (Ancestor is not null && Theirs is not null && !Compare(Ours, Merged).AreEqual) ||
                                  (Ancestor is not null && Ours is not null && !Compare(Ours, Merged).AreEqual);

    private bool IsTreeConflict => (AddedOrEdited || AnyRename) && (IsDeletion || OurRootDeletedParent is not null || TheirRootDeletedParent is not null);

    private bool IsDeletion => Ancestor is not null && (Theirs is null || Ours is null);

    private bool AnyRename => Ancestor?.Path is not null &&
                             ((Ours?.Path is not null && !Ours.Path.Equals(Ancestor.Path)) ||
                              (Theirs?.Path is not null && !Theirs.Path.Equals(Ancestor.Path)));

    /// <summary>Gets the merged node.</summary>
    public TreeItem? Merged { get; private set; }

    /// <summary>Gets the node path.</summary>
    public DataPath Path { get; private set; }

    /// <summary>Gets the list of conflicts.</summary>
    public IImmutableList<MergeValueConflict> Conflicts { get; }

    private bool HasUnresolvedConflicts => Conflicts.Any(c => !c.IsResolved);

    /// <summary>Gets the merge policy.</summary>
    public ComparisonPolicy Policy { get; private set; }

    /// <summary>Gets the merge status.</summary>
    public ItemMergeStatus Status { get; internal set; }

    private Type CreateMergeInstance()
    {
        var nonNull = Ours ?? Theirs ?? Ancestor ?? throw new NullReferenceException();
        var type = nonNull.GetType();
        switch (nonNull)
        {
            case Node node:
                Merged = NodeFactory.Create(type, node.Id);
                Merged.Path = Ancestor?.Path ?? (Ours ?? Theirs)?.Path ?? throw new NullReferenceException();
                break;
            case Resource resource:
                Merged = new Resource(resource.ThrowIfNoPath(), new Resource.Data(System.IO.Stream.Null));
                break;
            default:
                throw new NotSupportedException();
        }
        return type;
    }

    private ImmutableList<MergeValueConflict>.Builder ProcessPropertyValues(Type type)
    {
        var conflicts = ImmutableList.CreateBuilder<MergeValueConflict>();
        foreach (var property in Comparer.GetProperties(type, Policy))
        {
            if (!TryMergePropertyValue(property,
                                       out var setter,
                                       out var ancestorValue,
                                       out var ourValue,
                                       out var theirValue))
            {
                void ResolveCallback(object value)
                {
                    setter(Merged, value);
                    UpdateStatus();
                }
                var conflict = new MergeValueConflict(property,
                                                      ancestorValue,
                                                      ourValue,
                                                      theirValue,
                                                      ResolveCallback);
                conflicts.Add(conflict);
            }
        }

        return conflicts;
    }

    private DataPath GetMergePath()
    {
        if (AnyRename)
        {
            // If a rename operation was made on one branches, take the path
            // which is different than the ancestor path
            return Theirs?.Path is null || Theirs.Path.Equals(Ancestor!.Path) ? Ours!.Path! : Theirs.Path!;
        }
        else
        {
            return Ours?.Path ?? Theirs?.Path ?? Ancestor?.Path ??
                throw new NullReferenceException();
        }
    }

    internal Delegate Transform(IGitUpdateCommand command)
    {
        switch (Status)
        {
            case ItemMergeStatus.Add:
            case ItemMergeStatus.Edit:
                if (Merged is null)
                {
                    throw new InvalidOperationException("No merge value has been set.");
                }
                return command.CreateOrUpdate(Merged);
            case ItemMergeStatus.Rename:
                if (Merged is null)
                {
                    throw new InvalidOperationException("No merge value has been set.");
                }
                if (Path is null)
                {
                    throw new InvalidOperationException("No path has been set.");
                }
                return command.Rename(Merged, Path);
            case ItemMergeStatus.Delete:
                if (Ancestor is null)
                {
                    throw new InvalidOperationException("The deletion change does not contain any ancestor.");
                }
                return command.Delete(Ancestor.ThrowIfNoPath());
            default:
                throw new GitObjectDbException("Remaining conflicts.");
        }
    }

    private void UpdateStatus()
    {
        if (IsTreeConflict)
        {
            Status = ItemMergeStatus.TreeConflict;
        }
        else if (IsDeletion)
        {
            var nonNull = Theirs ?? Ours;
            Status = nonNull != null && !Compare(Ancestor, nonNull).AreEqual ?
                ItemMergeStatus.TreeConflict :
                ItemMergeStatus.Delete;
        }
        else if (HasUnresolvedConflicts)
        {
            Status = ItemMergeStatus.EditConflict;
        }
        else if (AnyRename)
        {
            Status = ItemMergeStatus.Rename;
        }
        else if (Compare(Ours, Merged).AreEqual)
        {
            Status = ItemMergeStatus.NoChange;
        }
        else
        {
            Status = Ancestor == null ? ItemMergeStatus.Add : ItemMergeStatus.Edit;
        }
    }

    private bool TryMergePropertyValue(PropertyInfo property,
                                       out MemberSetter setter,
                                       out object? ancestorValue,
                                       out object? ourValue,
                                       out object? theirValue)
    {
        var getter = Reflect.PropertyGetter(property);
        setter = Reflect.PropertySetter(property);
        ancestorValue = GetValue(Ancestor);
        ourValue = GetValue(Ours);
        theirValue = GetValue(Theirs);

        if (StillExists)
        {
            // Both values are equal -> no conflict
            if (Compare(ourValue, theirValue).AreEqual)
            {
                setter(Merged, ourValue);
                return true;
            }

            // Only changed in their changes -> no conflict
            if (Ancestor != null && Compare(ancestorValue, ourValue).AreEqual)
            {
                setter(Merged, theirValue);
                return true;
            }

            // Only changed in our changes -> no conflict
            if (Ancestor != null && Compare(ancestorValue, theirValue).AreEqual)
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
        throw new NotSupportedException("Expected execution path.");

        object? GetValue(TreeItem? item)
        {
            return item != null ? getter(item) : null;
        }
    }

    private ComparisonResult Compare(object? ancestorValue, object? theirValue)
    {
        return Comparer.CompareInternal(ancestorValue, theirValue, Policy);
    }
}
