using GitObjectDb.YamlDotNet.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace GitObjectDb.YamlDotNet.Core;
internal class NodeReferenceDeserializer : INodeDeserializer
{
    private readonly AsyncLocal<HashSet<IParser>> _preventReEntrant = new();

    private HashSet<IParser> PreventReEntrant => _preventReEntrant.Value ??= new();

    public bool Deserialize(IParser reader,
                            Type expectedType,
                            Func<IParser, Type, object?> nestedObjectDeserializer,
                            out object? value)
    {
        var preventReEntrant = PreventReEntrant;
        if (!preventReEntrant.Contains(reader) &&
            typeof(NodeReference).IsAssignableFrom(expectedType))
        {
            preventReEntrant.Add(reader);
            try
            {
                value = nestedObjectDeserializer.Invoke(reader, typeof(NodeReference)) ??
                    throw new YamlException($"{nameof(NodeReference)} could not be read.");
                return true;
            }
            finally
            {
                preventReEntrant.Remove(reader);
            }
        }
        else
        {
            value = null;
            return false;
        }
    }
}
