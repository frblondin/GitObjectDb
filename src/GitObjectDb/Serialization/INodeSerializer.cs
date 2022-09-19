using GitObjectDb.Model;
using GitObjectDb.Serialization.Json;
using LibGit2Sharp;
using System;
using System.IO;
using System.Text.Json;

namespace GitObjectDb.Serialization;

internal interface INodeSerializer
{
    public JsonSerializerOptions Options { get; set; }

    Node Deserialize(Stream stream,
                     ObjectId treeId,
                     DataPath path,
                     IDataModel model,
                     ItemLoader referenceResolver);

    Stream Serialize(Node node);
}
