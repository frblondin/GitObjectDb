using GitObjectDb.Model;
using GitObjectDb.YamlDotNet.Core;
using LibGit2Sharp;
using Microsoft.IO;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
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

        Model.UpdateBaseProperties(result, treeId, path);

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
        CheckForInvalidCharacters(pattern);
        return Regex.Escape(pattern);
    }

    /// <summary>
    /// YamlDotNet doesn't provide a clean way to escape a string value...
    /// so for now, we don't support search with string containing chars needing to be escaped.
    /// </summary>
    private void CheckForInvalidCharacters(string pattern)
    {
        var matches = pattern.Where(CharNeedingEscaping).ToList();
        if (matches.Any())
        {
            throw new GitObjectDbException($"Search pattern '{pattern}' contains characters needing to be escaped: {string.Join(", ", matches)}.");
        }

        static bool CharNeedingEscaping(char character) =>
            !IsPrintable(character) ||
            IsBreak(character) ||
            character == '"' ||
            character == '\\';
        static bool IsPrintable(char character) =>
            character == '\t' ||
            character == '\n' ||
            character == '\r' ||
            (character >= ' ' && character <= '~') ||
            character == '\u0085' ||
            (character >= '\u00a0' && character <= '\ud7ff') ||
            (character >= '\ue000' && character <= '\ufffd');
        static bool IsBreak(char character)
        {
            if (character <= '\r')
            {
                if (character != '\n' && character != '\r')
                {
                    return false;
                }
            }
            else if (character != '\u0085')
            {
                if (character != '\u2028' && character != '\u2029')
                {
                    return false;
                }
                return true;
            }
            return true;
        }
    }
}
