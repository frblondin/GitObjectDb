using Fasterflect;
using GitObjectDb.Model;
using GitObjectDb.SystemTextJson.Converters;
using GitObjectDb.SystemTextJson.Tools;
using GitObjectDb.Tools;
using LibGit2Sharp;
using Microsoft.IO;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace GitObjectDb.SystemTextJson;

internal partial class NodeSerializer : INodeSerializer
{
    private const string CommentStringToEscape = "*/";
    private const string CommentStringToUnescape = "ø/";
    private readonly RecyclableMemoryStreamManager _streamManager;
    private readonly JsonWriterOptions _writerOptions = new()
    {
        Indented = true,
    };

    public NodeSerializer(IDataModel model, Action<JsonSerializerOptions>? configure = null)
    {
        Model = model;
        _streamManager = new();

        Options = CreateSerializerOptions(model, configure);
    }

    public IDataModel Model { get; }

    public JsonSerializerOptions Options { get; set; }

    public string FileExtension => "json";

    private static JsonSerializerOptions CreateSerializerOptions(IDataModel model, Action<JsonSerializerOptions>? configure)
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
        result.Converters.Add(new UniqueIdConverter());

        configure?.Invoke(result);

        return result;
    }

    public Stream Serialize(Node node)
    {
        var result = _streamManager.GetStream();
        Serialize(node, result);
        result.Seek(0L, SeekOrigin.Begin);
        return result;
    }

    public void Serialize(Node node, Stream stream)
    {
        using var writer = new Utf8JsonWriter(stream, _writerOptions);
        JsonSerializer.Serialize(writer, node, Options);
        WriteEmbeddedResource(node, writer);
    }

    public Node Deserialize(Stream stream,
                            ObjectId treeId,
                            DataPath path,
                            INodeSerializer.ItemLoader referenceResolver)
    {
        var isRootContext = NodeReferenceHandler.CurrentContext.Value == null;
        var context =
            NodeReferenceHandler.CurrentContext.Value ??=
            new NodeReferenceHandler.DataContext(this, referenceResolver, treeId);
        try
        {
            var length = (int)stream.Length;
            var bytes = ArrayPool<byte>.Shared.Rent(length);
            try
            {
                stream.Read(bytes, 0, length);
                var document = JsonDocument.Parse(bytes.AsMemory(0, length), new()
                {
                    CommentHandling = JsonCommentHandling.Skip,
                });
                var result = JsonSerializer.Deserialize<Node>(document, Options)!;
                var embeddedResource = ReadEmbeddedResource(new(bytes, 0, length));
                Model.UpdateBaseProperties(result, treeId, path, embeddedResource);

                var newType = Model.GetNewTypeIfDeprecated(result.GetType());
                if (newType is not null)
                {
                    result = Model.UpdateDeprecatedNode(result, newType);

                    // Update back reference to modified node
                    context.Resolver!.AddReference(result.Path!.ToString(), result);
                }

                context.PostDeserializeationRefResolver.ReadReferencePaths(result, document, path);

                if (isRootContext)
                {
                    context.PostDeserializeationRefResolver.ResolveReferencesFromPaths(context, referenceResolver);
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
        using var writer = new Utf8JsonWriter(stream, new()
        {
            Encoder = Options.Encoder,
            SkipValidation = true,
        });
        writer.WriteStringValue(pattern);
    }
}
