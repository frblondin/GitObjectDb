using System.IO;

namespace GitObjectDb.Serialization.Json
{
    internal interface INodeSerializer
    {
        NonScalar Deserialize(Stream stream, DataPath path);

        Stream Serialize(Node node);
    }
}