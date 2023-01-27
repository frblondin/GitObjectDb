using GitObjectDb.Model;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading;

namespace GitObjectDb.SystemTextJson;

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
        public DataContext(NodeSerializer serializer, INodeSerializer.ItemLoader accessor, ObjectId treeId)
        {
            Accessor = accessor;
            TreeId = treeId;
            Resolver = new NodeReferenceResolver(this);
            PostDeserializeationRefResolver = new(serializer);
        }

        internal INodeSerializer.ItemLoader Accessor { get; }

        internal ObjectId TreeId { get; }

        internal NodeReferenceResolver Resolver { get; set; }

        internal NodeReferencePostDeserializationResolver PostDeserializeationRefResolver { get; }
    }
}
