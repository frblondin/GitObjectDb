using GitObjectDb.Model;
using GitObjectDb.Serialization.Json.Converters;
using LibGit2Sharp;
using Microsoft.IO;
using System;
using System.Buffers;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitObjectDb.Serialization.Json;

internal partial class NodeSerializer : INodeSerializer
{
    private const string CommentStringToEscape = "*/";
    private const string CommentStringToUnescape = "ø/";

    private readonly RecyclableMemoryStreamManager _streamManager;
    private readonly JsonWriterOptions _writerOptions = new()
    {
        Indented = true,
    };

    public NodeSerializer(RecyclableMemoryStreamManager streamManager)
    {
        Options = NodeSerializer.CreateSerializerOptions();
        _streamManager = streamManager;
    }

    public JsonSerializerOptions Options { get; set; }

    internal static JsonSerializerOptions CreateSerializerOptions()
    {
        var result = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            ReferenceHandler = new NodeReferenceHandler(),
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        result.Converters.Add(new NonScalarConverter());
        result.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        return result;
    }

    public Stream Serialize(Node node)
    {
        var result = _streamManager.GetStream();
        using (var writer = new Utf8JsonWriter(result, _writerOptions))
        {
            JsonSerializer.Serialize(writer, new NonScalar(node), Options);
            WriteEmbeddedResource(node, writer);
        }
        result.Seek(0L, SeekOrigin.Begin);
        return result;
    }

    public Node Deserialize(Stream stream,
                            ObjectId treeId,
                            DataPath? path,
                            IDataModel model,
                            ItemLoader referenceResolver)
    {
        NodeReferenceHandler.CurrentContext.Value = new NodeReferenceHandler.DataContext(referenceResolver, treeId);
        try
        {
            var length = (int)stream.Length;
            var bytes = ArrayPool<byte>.Shared.Rent(length);
            try
            {
                stream.Read(bytes, 0, length);
                var span = new ReadOnlySpan<byte>(bytes, 0, length);
                var result = JsonSerializer.Deserialize<NonScalar>(span, Options)!.Node;
                var embeddedResource = ReadEmbeddedResource(new ReadOnlySequence<byte>(bytes, 0, length));
                result = UpdateBaseProperties(result, path, embeddedResource);

                var newType = model.GetNewTypeIfDeprecated(result.GetType());
                if (newType is not null)
                {
                    return UpdateDeprecatedNode(result, newType, model);
                }

                return result;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bytes);
            }
        }
        finally
        {
            NodeReferenceHandler.CurrentContext.Value = null;
        }
    }

    private static Node UpdateDeprecatedNode(Node deprecated, Type newType, IDataModel model)
    {
        if (model.DeprecatedNodeUpdater is null)
        {
            throw new GitObjectDbException("No deprecated node updater defined in model.");
        }
        var updated = model.DeprecatedNodeUpdater(deprecated, newType) ??
            throw new GitObjectDbException("Deprecated node updater did not return any value.");
        if (!newType.IsInstanceOfType(updated))
        {
            throw new GitObjectDbException($"Deprecated node updater did not return a value of type '{newType}'.");
        }
        if (updated.Id != deprecated.Id)
        {
            throw new GitObjectDbException($"Updated node does not have the same id.");
        }
        return UpdateBaseProperties(updated, deprecated.Path, deprecated.EmbeddedResource);
    }

    private static Node UpdateBaseProperties(Node result, DataPath? path, string? embeddedResource) => result with
    {
        Path = path,
        EmbeddedResource = embeddedResource,
    };
}
