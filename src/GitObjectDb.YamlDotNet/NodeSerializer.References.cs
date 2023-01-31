using Fasterflect;
using GitObjectDb.Model;
using GitObjectDb.Tools;
using GitObjectDb.YamlDotNet.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GitObjectDb.YamlDotNet;

internal partial class NodeSerializer : INodeSerializer
{
    private void ResolveReferences(NodeReferenceParser parser)
    {
        while (parser.ReferencesToBeResolved.Count > 0)
        {
            var reference = parser.ReferencesToBeResolved[0];
            reference.ResolveReference();
            parser.ReferencesToBeResolved.RemoveAt(0);
        }

        CopyReferencesFromDeprecatedNodes(parser);
    }

    private void CopyReferencesFromDeprecatedNodes(NodeReferenceParser parser)
    {
        foreach (var kvp in parser.UpdatedDeprecatedNodes)
        {
            var oldTypeDescription = Model.GetDescription(kvp.Key.GetType());
            var newTypeDescription = Model.GetDescription(kvp.Value.GetType());
            CopyReferencePropertiesFromDeprecatedNode(kvp, oldTypeDescription, newTypeDescription);
        }
    }

    private static void CopyReferencePropertiesFromDeprecatedNode(KeyValuePair<Node, Node> kvp,
                                                                  NodeTypeDescription oldTypeDescription,
                                                                  NodeTypeDescription newTypeDescription)
    {
        foreach (var oldProperty in oldTypeDescription.SerializableProperties)
        {
            if (oldProperty.PropertyType.IsNode() ||
                oldProperty.PropertyType.IsNodeEnumerable(out var _))
            {
                var matchingProperty = newTypeDescription.SerializableProperties.FirstOrDefault(DoesPropertyMatch);
                if (matchingProperty is not null)
                {
                    var oldValue = Reflect.PropertyGetter(oldProperty).Invoke(kvp.Key);
                    if (oldValue is not null)
                    {
                        Reflect.PropertySetter(matchingProperty).Invoke(kvp.Value, oldValue);
                    }
                }

                bool DoesPropertyMatch(PropertyInfo property) =>
                    property.Name.Equals(oldProperty.Name, StringComparison.OrdinalIgnoreCase) &&
                    property.PropertyType.IsAssignableFrom(oldProperty.PropertyType);
            }
        }
    }
}
