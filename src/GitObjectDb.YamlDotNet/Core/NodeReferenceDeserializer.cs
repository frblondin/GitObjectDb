using GitObjectDb.YamlDotNet.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace GitObjectDb.YamlDotNet.Core;
internal class NodeReferenceDeserializer : INodeDeserializer
{
    private readonly HashSet<IParser> _preventReEntrant = new();

    public bool Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value)
    {
        if (!_preventReEntrant.Contains(reader) &&
            typeof(NodeReference).IsAssignableFrom(expectedType))
        {
            DeserializeReference(reader, nestedObjectDeserializer, out value);
            return true;
        }
        else
        {
            value = null;
            return false;
        }
    }

    private void DeserializeReference(IParser reader, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value)
    {
        var referenceReader = reader as NodeReferenceParser ??
            throw new NotSupportedException($"{nameof(NodeReferenceParser)} instance expected.");
        try
        {
            _preventReEntrant.Add(reader);
            var result = DeserializeReferenceImpl(reader, nestedObjectDeserializer, referenceReader);
            value = result;
        }
        finally
        {
            _preventReEntrant.Remove(reader);
        }
    }

    private static TreeItem DeserializeReferenceImpl(IParser reader, Func<IParser, Type, object?> nestedObjectDeserializer, NodeReferenceParser referenceReader)
    {
        var deserialized = nestedObjectDeserializer.Invoke(reader, typeof(object)) ??
            throw new YamlException($"{nameof(NodeReference)} could not be read.");
        return deserialized switch
        {
            NodeReference reference => ProcessReference(reference),
            TreeItem item => item,
            _ => throw new NotSupportedException(),
        };

        TreeItem ProcessReference(NodeReference reference)
        {
            var path = reference.Path ??
                throw new NotSupportedException($"{nameof(NodeReference)}.{nameof(NodeReference.Path)} does not contain a valid path.");
            var result =
                NodeReferenceParser.CurrentInstance.Nodes.FirstOrDefault(n => path.Equals(n.Path)) ??
                referenceReader.ReferenceResolver.Invoke(path);
            result.Path = path;
            return result;
        }
    }
}
