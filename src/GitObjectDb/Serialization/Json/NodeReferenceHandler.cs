using GitObjectDb.Model;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;

namespace GitObjectDb.Serialization.Json;

/// <summary>
/// <summary>Represents a method that creates a <see cref="ITreeItem"/> from a path.</summary>
/// </summary>
/// <param name="path">The path of item.</param>
/// <returns>An item.</returns>
public delegate ITreeItem ItemLoader(DataPath path);

internal class NodeReferenceHandler : ReferenceHandler
{
    internal static AsyncLocal<DataContext?> CurrentContext { get; } =
        new AsyncLocal<DataContext?>();

    public override ReferenceResolver CreateResolver()
    {
        var context = CurrentContext.Value;
        return context?.Resolver ?? new NodeReferenceResolver(context);
    }

    internal class DataContext
    {
        public DataContext(ItemLoader accessor, ObjectId treeId)
        {
            Accessor = accessor;
            TreeId = treeId;
            Resolver = new NodeReferenceResolver(this);
        }

        internal ItemLoader Accessor { get; }

        internal ObjectId TreeId { get; }

        internal ReferenceResolver? Resolver { get; set; }
    }
}
