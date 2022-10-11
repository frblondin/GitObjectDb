using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace GitObjectDb.SystemTextJson;

internal class NodeReferenceResolver : ReferenceResolver
{
    private readonly IDictionary<DataPath, TreeItem> _items = new Dictionary<DataPath, TreeItem>();
    private readonly NodeReferenceHandler.DataContext? _context;

    public NodeReferenceResolver(NodeReferenceHandler.DataContext? context)
    {
        _context = context;
    }

    public override void AddReference(string referenceId, object value)
    {
        if (value is TreeItem item && DataPath.TryParse(referenceId, out var path))
        {
            _items[path!] = item;
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

            alreadyExists = _items.Count != 0; // Only first node should be stored, others should be pointed to through a ref
            if (!_items.ContainsKey(item.Path))
            {
                _items[item.Path] = item;
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
        if (DataPath.TryParse(referenceId, out var path))
        {
            if (!_items.TryGetValue(path!, out var item))
            {
                if (_context is null)
                {
                    throw new NotSupportedException("The node accessor could not be found.");
                }
                _items[path!] = item = _context.Accessor(path!);
            }
            return item;
        }
        else
        {
            throw new NotSupportedException("Only data path reference is supported.");
        }
    }
}
