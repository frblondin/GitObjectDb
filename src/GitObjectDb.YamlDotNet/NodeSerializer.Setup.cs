using GitObjectDb.Model;
using GitObjectDb.YamlDotNet.Converters;
using GitObjectDb.YamlDotNet.Core;
using GitObjectDb.YamlDotNet.Model;
using System;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NodeDeserializers;
using YamlDotNet.Serialization.ObjectFactories;
using YamlDotNet.Serialization.TypeResolvers;
using NodeFactory = GitObjectDb.YamlDotNet.Core.NodeFactory;

namespace GitObjectDb.YamlDotNet;

public partial class NodeSerializer : INodeSerializer
{
    private static ISerializer CreateSerializer(IDataModel model,
                                                INamingConvention namingConvention,
                                                Action<SerializerBuilder>? configure)
    {
        var result = new SerializerBuilder()
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults |
                                            DefaultValuesHandling.OmitEmptyCollections)
            .DisableAliases()
            .WithTypeInspector(inner => new IgnoreDataMemberTypeInspector(inner))
            .WithTypeInspector(inner => new SortedReadablePropertiesTypeInspector(new DynamicTypeResolver(), false),
                               w => w.OnBottom())
            .WithObjectGraphTraversalStrategyFactory(
                NodeReferenceGraphTraversalStrategy.CreateFactory(namingConvention));

        ConfigureCommon(result, model, namingConvention);

        configure?.Invoke(result);

        return result.Build();
    }

    private static IDeserializer CreateDeserializer(IDataModel model,
                                                    INamingConvention namingConvention,
                                                    Action<DeserializerBuilder>? configure)
    {
        var objectFactory = new NodeFactory(new DefaultObjectFactory());
        var result = new DeserializerBuilder()
            .WithNodeDeserializer(new NodeReferenceDeserializer(), w => w.Before<ObjectNodeDeserializer>())
            .WithObjectFactory(objectFactory);

        ConfigureCommon(result, model, namingConvention);
        configure?.Invoke(result);

        return result.Build();
    }

    private static void ConfigureCommon<TBuilder>(BuilderSkeleton<TBuilder> builder,
                                                  IDataModel model,
                                                  INamingConvention namingConvention)
        where TBuilder : BuilderSkeleton<TBuilder>
    {
        builder
            .IgnoreFields()
            .WithNamingConvention(namingConvention)
            .WithTypeConverter(new DataPathConverter())
            .WithTypeConverter(new UniqueIdConverter())
            .WithTagMapping(ReferenceTag, typeof(NodeReference));

        foreach (var nodeType in model.NodeTypes)
        {
            var tag = nodeType.Type.GetYamlName();
            builder.WithTagMapping($"!{tag}", nodeType.Type);
        }
    }
}
