using GitObjectDb.Model;
using GitObjectDb.YamlDotNet.Core;
using LibGit2Sharp;
using Microsoft.IO;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace GitObjectDb.YamlDotNet;

internal partial class NodeSerializer : INodeSerializer
{
    internal const string ReferenceTag = "!$Reference";
    private readonly RecyclableMemoryStreamManager _streamManager;

    public NodeSerializer(IDataModel model,
                          INamingConvention namingConvention,
                          Action<SerializerBuilder>? configureSerializer = null,
                          Action<DeserializerBuilder>? configureDeserializer = null)
    {
        Model = model;
        _streamManager = new();

        Serializer = CreateSerializer(model, namingConvention, configureSerializer);
        Deserializer = CreateDeserializer(model, namingConvention, configureDeserializer);
    }

    public IDataModel Model { get; }

    public ISerializer Serializer { get; set; }

    public IDeserializer Deserializer { get; set; }

    public string FileExtension => "yaml";

    public Stream Serialize(Node node)
    {
        var result = _streamManager.GetStream();
        Serialize(node, result);
        return result;
    }

    public void Serialize(Node node, Stream stream)
    {
        using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, leaveOpen: true))
        {
            var emitter = new NodeReferenceEmitter(writer, node);
            Serializer.Serialize(emitter, node);
            WriteEmbeddedResource(emitter, node);
        }
        stream.Seek(0L, SeekOrigin.Begin);
    }

    public Node Deserialize(Stream stream,
                            ObjectId treeId,
                            DataPath path,
                            INodeSerializer.ItemLoader referenceResolver)
    {
        using var reader = new StreamReader(stream);
        using var parser = new NodeReferenceParser(reader, referenceResolver);
        var result = Deserializer.Deserialize<Node>(parser);

        var embeddedResource = ReadEmbeddedResource(stream);
        Model.UpdateBaseProperties(result, treeId, path, embeddedResource);

        result = UpdateIfDeprecated(parser, result);

        if (parser.IsRoot)
        {
            ResolveReferences(parser);
        }

        return result;
    }

    private Node UpdateIfDeprecated(NodeReferenceParser parser, Node result)
    {
        var newType = Model.GetNewTypeIfDeprecated(result.GetType());
        if (newType is not null)
        {
            parser.Nodes.Remove(result);
            var old = result;
            result = Model.UpdateDeprecatedNode(result, newType);
            parser.Nodes.Add(result);
            parser.UpdatedDeprecatedNodes.Add(new(old, result));
        }

        return result;
    }

    public string EscapeRegExPattern(string pattern)
    {
        var yamlPattern = ConvertToYamlValue(pattern);
        return pattern.Equals(yamlPattern, StringComparison.Ordinal) ?
               pattern :
               $"{Regex.Escape(pattern)}|{Regex.Escape(yamlPattern)}";
    }

    private string ConvertToYamlValue(string pattern)
    {
        var stream = _streamManager.GetStream();
        WriteYamlValue(pattern, stream);
        stream.Position = 0L;
        using var reader = new StreamReader(stream);
        var yamlValue = reader.ReadToEnd();
        return yamlValue.Substring(1, yamlValue.Length - 2); // Remove double quotes
    }

    private void WriteYamlValue(string pattern, Stream stream)
    {
        using var writer = new StreamWriter(stream, Encoding.UTF8, 1024, leaveOpen: true);
        var emitter = NodeReferenceEmitter.CreateEmitter(writer);
        emitter.Emit(new Scalar(pattern));
    }
}
