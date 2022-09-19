using GitObjectDb.Model;
using System;
using System.IO;

namespace GitObjectDb.Serialization
{
    internal interface INodeSerializer
    {
        Node Deserialize(Stream stream, DataPath? path, IDataModel model, Func<DataPath, ITreeItem> referenceResolver);

        Stream Serialize(Node node);

        string BindToName(Type type);

        Type BindToType(string fullTypeName);
    }
}