using System;
using System.IO;

namespace GitObjectDb.Serialization
{
    internal interface INodeSerializer
    {
        NonScalar Deserialize(Stream stream, DataPath path, string sha, Func<DataPath, ITreeItem> referenceResolver);

        Stream Serialize(Node node);

        string BindToName(Type type);

        Type BindToType(string fullTypeName);
    }
}