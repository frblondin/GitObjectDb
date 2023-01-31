using GitObjectDb.YamlDotNet.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace GitObjectDb.YamlDotNet.Core;
internal class NodeReferenceDeserializer : INodeDeserializer
{
    private readonly HashSet<IParser> _preventReEntrant = new();

    public bool Deserialize(IParser reader,
                            Type expectedType,
                            Func<IParser, Type, object?> nestedObjectDeserializer,
                            out object? value)
    {
        if (!_preventReEntrant.Contains(reader) &&
            typeof(NodeReference).IsAssignableFrom(expectedType))
        {
            _preventReEntrant.Add(reader);
            try
            {
                value = nestedObjectDeserializer.Invoke(reader, typeof(NodeReference)) ??
                    throw new YamlException($"{nameof(NodeReference)} could not be read.");
                return true;
            }
            finally
            {
                _preventReEntrant.Remove(reader);
            }
        }
        else
        {
            value = null;
            return false;
        }
    }
}
