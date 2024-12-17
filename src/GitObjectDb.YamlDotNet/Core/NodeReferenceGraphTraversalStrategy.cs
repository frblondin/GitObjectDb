using GitObjectDb.YamlDotNet.Model;
using System.Collections.Generic;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.ObjectFactories;
using YamlDotNet.Serialization.ObjectGraphTraversalStrategies;

namespace GitObjectDb.YamlDotNet.Core;
internal class NodeReferenceGraphTraversalStrategy : FullObjectGraphTraversalStrategy
{
    public NodeReferenceGraphTraversalStrategy(ITypeInspector typeDescriptor,
                                               ITypeResolver typeResolver,
                                               int maxRecursion,
                                               INamingConvention namingConvention,
                                               IObjectFactory objectFactory)
        : base(typeDescriptor, typeResolver, maxRecursion, namingConvention, objectFactory)
    {
    }

    internal static ObjectGraphTraversalStrategyFactory CreateFactory(INamingConvention namingConvention) =>
        (typeInspector, typeResolver, typeConverters, maximumRecursion) =>
        new NodeReferenceGraphTraversalStrategy(typeInspector,
                                                typeResolver,
                                                maximumRecursion,
                                                namingConvention,
                                                new DefaultObjectFactory());

    protected override void TraverseObject<TContext>(IPropertyDescriptor? propertyDescriptor,
                                                     IObjectDescriptor value,
                                                     IObjectGraphVisitor<TContext> visitor,
                                                     TContext context,
                                                     Stack<ObjectPathSegment> path,
                                                     ObjectSerializer serializer)
    {
        if (value.Value is Node node &&
            context is NodeReferenceEmitter nodeEmitter &&
            !nodeEmitter.ShouldTraverse(node, context))
        {
            var reference = new NodeReference { Path = node.Path };
            var description = new ObjectDescriptor(reference, reference.GetType(), reference.GetType());
            base.TraverseObject(propertyDescriptor, description, visitor, context, path, serializer);
        }
        else
        {
            base.TraverseObject(propertyDescriptor, value, visitor, context, path, serializer);
        }
    }
}
