using GitObjectDb.Model;
using GitObjectDb.Serialization.Json;
using LibGit2Sharp;
using System;
using System.IO;

namespace GitObjectDb.Serialization;

internal interface INodeSerializer
{
    Node Deserialize(Stream stream,
                     ObjectId treeId,
                     DataPath path,
                     IDataModel model,
                     ItemLoader referenceResolver);

    Stream Serialize(Node node);
}
