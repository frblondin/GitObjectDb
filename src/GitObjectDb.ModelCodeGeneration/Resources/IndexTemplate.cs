using System;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable SA1600 // Elements must be documented
#pragma warning disable CA1050 // Declare types in namespaces
#pragma warning disable CA1710 // Identifiers should have correct suffix
public partial class ModelTemplate : GitObjectDb.Models.IObjectRepositoryIndex
#pragma warning restore CA1710 // Identifiers should have correct suffix
#pragma warning restore CA1050 // Declare types in namespaces
#pragma warning restore SA1600 // Elements must be documented
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
{
    private System.Collections.Immutable.ImmutableSortedDictionary<string, System.Collections.Immutable.ImmutableSortedSet<string>> _values;

    /// <inheritdoc/>
    [System.Runtime.Serialization.DataMember]
    public System.Collections.Immutable.ImmutableSortedDictionary<string, System.Collections.Immutable.ImmutableSortedSet<string>> Values
    {
        get
        {
            if (_values == null)
            {
                _values = FullScan();
            }
            return _values;
        }

        private set
        {
            _values = value;
        }
    }

    /// <inheritdoc/>
    public int Count => Values.Count;

    /// <inheritdoc/>
    public System.Collections.Generic.IEnumerable<GitObjectDb.Models.IModelObject> this[string key]
    {
        get
        {
            Values.TryGetValue(key, out var result);
            if (result != null)
            {
                foreach (var path in result)
                {
                    yield return Repository.GetFromGitPath(path);
                }
            }
        }
    }

    /// <inheritdoc />
    public bool Contains(string key) => Values.TryGetValue(key, out var result) && result?.Count > 0;

    /// <inheritdoc />
    public System.Collections.Immutable.ImmutableSortedDictionary<string, System.Collections.Immutable.ImmutableSortedSet<string>> FullScan()
    {
        var result = new System.Collections.Generic.SortedDictionary<string, System.Collections.Generic.ISet<string>>();
        if (Repository != null)
        {
            foreach (var node in GitObjectDb.Models.IModelObjectExtensions.Flatten(Repository))
            {
                UpdateAdded(node, result);
            }
        }
        return System.Collections.Immutable.ImmutableSortedDictionary.ToImmutableSortedDictionary(result,
            kvp => kvp.Key,
            kvp => System.Collections.Immutable.ImmutableSortedSet.ToImmutableSortedSet(kvp.Value));
    }

    /// <inheritdoc />
    public System.Collections.Immutable.ImmutableSortedDictionary<string, System.Collections.Immutable.ImmutableSortedSet<string>> Update(GitObjectDb.Models.Compare.ObjectRepositoryChangeCollection changes)
    {
        var builder = System.Collections.Immutable.ImmutableSortedDictionary.CreateBuilder<string, System.Collections.Generic.ISet<string>>();
        builder.AddRange(
            System.Linq.Enumerable.Select(
                Values,
                kvp => new System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.ISet<string>>(
                    kvp.Key,
                    new System.Collections.Generic.SortedSet<string>(kvp.Value))));
        var anyUpdate = false;
        foreach (var change in changes)
        {
            switch (change.Status)
            {
                case LibGit2Sharp.ChangeKind.Added:
                    anyUpdate |= UpdateAdded(change.New, builder);
                    continue;
                case LibGit2Sharp.ChangeKind.Deleted:
                    anyUpdate |= UpdateDeleted(change.Old, builder);
                    continue;
                case LibGit2Sharp.ChangeKind.Modified:
                    anyUpdate |= UpdateModified(change.Old, change.New, builder);
                    continue;
                default:
                    throw new NotSupportedException();
            }
        }
        return anyUpdate ?
            System.Collections.Immutable.ImmutableSortedDictionary.ToImmutableSortedDictionary(builder,
                kvp => kvp.Key,
                kvp => System.Collections.Immutable.ImmutableSortedSet.ToImmutableSortedSet(kvp.Value)) :
            null;
    }

    private bool UpdateAdded(GitObjectDb.Models.IModelObject node, System.Collections.Generic.IDictionary<string, System.Collections.Generic.ISet<string>> result)
    {
        var path = new Lazy<string>(() => GitObjectDb.Models.IModelObjectExtensions.GetFolderPath(node));
        var keys = new System.Collections.Generic.SortedSet<string>();
        ComputeKeys(node, keys);

        var anyUpdate = false;
        foreach (var key in keys)
        {
            var set = GetIndexValues(key, result, create: true);
            anyUpdate |= set.Add(path.Value);
        }
        return anyUpdate;
    }

    private bool UpdateDeleted(GitObjectDb.Models.IModelObject node, System.Collections.Generic.IDictionary<string, System.Collections.Generic.ISet<string>> result)
    {
        var path = new Lazy<string>(() => GitObjectDb.Models.IModelObjectExtensions.GetFolderPath(node));
        var keys = new System.Collections.Generic.SortedSet<string>();
        ComputeKeys(node, keys);

        var anyUpdate = false;
        foreach (var key in keys)
        {
            if (key == null)
            {
                continue;
            }
            var set = GetIndexValues(key, result, create: false);
            anyUpdate |= set?.Remove(path.Value) ?? false;
        }
        return anyUpdate;
    }

    private bool UpdateModified(GitObjectDb.Models.IModelObject old, GitObjectDb.Models.IModelObject @new, System.Collections.Generic.IDictionary<string, System.Collections.Generic.ISet<string>> result)
    {
        var oldKeys = new System.Collections.Generic.SortedSet<string>();
        var newKeys = new System.Collections.Generic.SortedSet<string>();
        ComputeKeys(old, oldKeys);
        ComputeKeys(@new, newKeys);
        var path = new Lazy<string>(() => GitObjectDb.Models.IModelObjectExtensions.GetFolderPath(old));

        var anyUpdate = false;
        foreach (var key in System.Linq.Enumerable.Except(oldKeys, newKeys))
        {
            if (key == null)
            {
                continue;
            }
            var set = GetIndexValues(key, result, create: false);
            anyUpdate |= set?.Remove(path.Value) ?? false;
        }
        foreach (var key in System.Linq.Enumerable.Except(newKeys, oldKeys))
        {
            var set = GetIndexValues(key, result, create: true);
            anyUpdate |= set.Add(path.Value);
        }
        return anyUpdate;
    }

    private static System.Collections.Generic.ISet<string> GetIndexValues(string key, System.Collections.Generic.IDictionary<string, System.Collections.Generic.ISet<string>> dictionary, bool create)
    {
        if (!dictionary.TryGetValue(key, out var result) && create)
        {
            dictionary[key] = result = new System.Collections.Generic.SortedSet<string>();
        }
        return result;
    }

    /// <summary>
    /// Computes the key sequence for a <paramref name="node"/>.
    /// </summary>
    /// <param name="node">The node to be analyzed.</param>
    /// <param name="result">The key values.</param>
    partial void ComputeKeys(GitObjectDb.Models.IModelObject node, System.Collections.Generic.ISet<string> result);
}
