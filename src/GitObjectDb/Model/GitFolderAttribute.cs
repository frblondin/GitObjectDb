using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace GitObjectDb.Model;

/// <summary>Instructs the engine in which folder name to store nodes.</summary>
/// <seealso cref="Attribute" />
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class GitFolderAttribute : Attribute
{
    /// <summary>Gets the default value of <see cref="UseNodeFolders"/>.</summary>
    public const bool DefaultUseNodeFoldersValue = true;

    private static readonly ConcurrentDictionary<Type, GitFolderAttribute> _cache = new();

    /// <summary>Gets the name of the folder.</summary>
    public string? FolderName { get; init; }

    /// <summary>Gets a value indicating whether node should be stored in a nested folder (FolderName/NodeId/data.json) or not (FolderName/NodeId.json).</summary>
    public bool UseNodeFolders { get; init; } = DefaultUseNodeFoldersValue;

    internal static GitFolderAttribute Get(Type type) =>
        _cache.GetOrAdd(type, type => type.GetCustomAttribute<GitFolderAttribute>(true));
}
