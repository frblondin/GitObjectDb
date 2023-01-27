using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace GitObjectDb.SystemTextJson;

internal class NodeReferenceResolver : ReferenceResolver
{
    private readonly NodeReferenceHandler.DataContext? _context;

    public NodeReferenceResolver(NodeReferenceHandler.DataContext? context)
    {
        _context = context;
    }

    internal IDictionary<DataPath, TreeItem> Items { get; } = new Dictionary<DataPath, TreeItem>();

    public override void AddReference(string referenceId, object value)
    {
        if (value is TreeItem item && DataPath.TryParse(referenceId, out var path))
        {
            Items[path!] = item;
        }
    }

    public override string GetReference(object value, out bool alreadyExists)
    {
        if (value is TreeItem item)
        {
            if (item.Path is null)
            {
                throw new GitObjectDbException("The path has not been set for current item.");
            }

            alreadyExists = Items.Count != 0; // Only first node should be stored, others should be pointed to through a ref
            if (!Items.ContainsKey(item.Path))
            {
                Items[item.Path] = item;
            }
            return item.Path.FilePath;
        }
        else
        {
            alreadyExists = false;
            return RuntimeHelpers.GetHashCode(value).ToString();
        }
    }

    public override object ResolveReference(string referenceId)
    {
        // Because of type depreciation management (e.g. node types decorated with IsDeprecatedNodeTypeAttribute),
        // we need to resolve nodes without references first and then resolve direct references
        // (see NodeSerializer.ReadReferencePaths and NodeSerializer.ResolveReferencesFromPaths)
        return null!;
    }
}
