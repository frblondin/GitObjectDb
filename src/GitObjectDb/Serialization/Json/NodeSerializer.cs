using GitObjectDb.Injection;
using GitObjectDb.Internal;
using GitObjectDb.Model;
using GitObjectDb.Serialization.Json.Converters;
using LibGit2Sharp;
using Microsoft.IO;
using System;
using System.Buffers;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

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

    [FactoryDelegateConstructor(typeof(ConnectionFactory))]
    public NodeSerializer(IDataModel model, RecyclableMemoryStreamManager streamManager)
    {
        Model = model;
        _streamManager = streamManager;

        Options = CreateSerializerOptions(model);
    }

    public IDataModel Model { get; }

    public JsonSerializerOptions Options { get; set; }

    private static JsonSerializerOptions CreateSerializerOptions(IDataModel model)
    {
        var result = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            ReferenceHandler = new NodeReferenceHandler(),
            ReadCommentHandling = JsonCommentHandling.Skip,
            TypeInfoResolver = new NodeTypeInfoResolver(model),
        };

        result.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        result.Converters.Add(new TimeZoneInfoConverter());

        return result;
    }

    public Stream Serialize(Node node)
    {
        var result = _streamManager.GetStream();
        using (var writer = new Utf8JsonWriter(result, _writerOptions))
        {
            JsonSerializer.Serialize(writer, node, Options);
            WriteEmbeddedResource(node, writer);
        }
        result.Seek(0L, SeekOrigin.Begin);
        return result;
    }

    public Node Deserialize(Stream stream,
                            ObjectId treeId,
                            DataPath? path,
                            ItemLoader referenceResolver)
    {
        var isRootContext = NodeReferenceHandler.CurrentContext.Value == null;
        NodeReferenceHandler.CurrentContext.Value ??= new NodeReferenceHandler.DataContext(referenceResolver, treeId);
        try
        {
            var length = (int)stream.Length;
            var bytes = ArrayPool<byte>.Shared.Rent(length);
            try
            {
                stream.Read(bytes, 0, length);
                var reader = new Utf8JsonReader(bytes, new()
                {
                    CommentHandling = JsonCommentHandling.Skip,
                });
                var result = JsonSerializer.Deserialize<Node>(ref reader, Options)!;
                var embeddedResource = ReadEmbeddedResource(new ReadOnlySequence<byte>(bytes, 0, length));
                result = Model.UpdateBaseProperties(result, path, embeddedResource);

                var newType = Model.GetNewTypeIfDeprecated(result.GetType());
                if (newType is not null)
                {
                    return Model.UpdateDeprecatedNode(result, newType);
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
            if (isRootContext)
            {
                NodeReferenceHandler.CurrentContext.Value = null;
            }
        }
    }

    public string EscapeRegExPattern(string pattern)
    {
        var jsonPattern = ConvertToJsonValue(pattern);
        return pattern.Equals(jsonPattern, StringComparison.Ordinal) ?
               pattern :
               $"{Regex.Escape(pattern)}|{Regex.Escape(jsonPattern)}";
    }

    private string ConvertToJsonValue(string pattern)
    {
        var stream = _streamManager.GetStream();
        WriteJsonValue(pattern, stream);
        stream.Position = 0L;
        using var reader = new StreamReader(stream);
        var jsonValue = reader.ReadToEnd();
        return jsonValue.Substring(1, jsonValue.Length - 2); // Remove double quotes
    }

    private void WriteJsonValue(string pattern, Stream stream)
    {
        var options = new JsonWriterOptions
        {
            Encoder = Options.Encoder,
            SkipValidation = true,
        };
        using var writer = new Utf8JsonWriter(stream, options);
        writer.WriteStringValue(pattern);
    }
}
