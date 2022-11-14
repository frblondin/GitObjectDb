using GitObjectDb.Model;
using GitObjectDb.YamlDotNet.Converters;
using GitObjectDb.YamlDotNet.Core;
using GitObjectDb.YamlDotNet.Model;
using LibGit2Sharp;
using Microsoft.IO;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NodeDeserializers;
using YamlDotNet.Serialization.ObjectFactories;
using YamlDotNet.Serialization.TypeInspectors;
using YamlDotNet.Serialization.TypeResolvers;

namespace GitObjectDb.YamlDotNet;

internal partial class NodeSerializer : INodeSerializer
{
    private readonly RecyclableMemoryStreamManager _streamManager;

    public NodeSerializer(IDataModel model, INamingConvention namingConvention, Action<SerializerBuilder>? configureSerializer = null, Action<DeserializerBuilder>? configureDeserializer = null)
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

    private static ISerializer CreateSerializer(IDataModel model, INamingConvention namingConvention, Action<SerializerBuilder>? configure)
    {
        var result = new SerializerBuilder()
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults |
                                            DefaultValuesHandling.OmitEmptyCollections)
            .DisableAliases()
            .WithTypeInspector(inner => new IgnoreDataMemberTypeInspector(inner))
            .WithTypeInspector(inner => new SortedReadablePropertiesTypeInspector(inner, new DynamicTypeResolver()), w => w.OnBottom())
            .WithObjectGraphTraversalStrategyFactory(
                NodeReferenceGraphTraversalStrategy.CreateFactory(namingConvention));

        ConfigureCommon(result, model, namingConvention);

        configure?.Invoke(result);

        return result.Build();
    }

    private static IDeserializer CreateDeserializer(IDataModel model, INamingConvention namingConvention, Action<DeserializerBuilder>? configure)
    {
        var result = new DeserializerBuilder()
            .WithNodeDeserializer(new NodeReferenceDeserializer(), w => w.Before<ObjectNodeDeserializer>())
            .WithObjectFactory(new NodeFactory(new DefaultObjectFactory()));

        ConfigureCommon(result, model, namingConvention);

        configure?.Invoke(result);

        return result.Build();
    }

    private static void ConfigureCommon<TBuilder>(BuilderSkeleton<TBuilder> builder, IDataModel model, INamingConvention namingConvention)
        where TBuilder : BuilderSkeleton<TBuilder>
    {
        builder
            .IgnoreFields()
            .WithNamingConvention(namingConvention)
            .WithTypeConverter(new DataPathConverter())
            .WithTypeConverter(new UniqueIdConverter())
            .WithTagMapping("!$Reference", typeof(NodeReference));

        foreach (var nodeType in model.NodeTypes)
        {
            var tag = nodeType.Type
                .FullName
                .Replace('+', '.');
            builder.WithTagMapping($"!{tag}", nodeType.Type);
        }
    }

    public Stream Serialize(Node node)
    {
        var result = _streamManager.GetStream();
        using (var writer = new StreamWriter(result, Encoding.UTF8, 1024, leaveOpen: true))
        {
            var emitter = new NodeReferenceEmitter(writer, node);
            Serializer.Serialize(emitter, node);
            WriteEmbeddedResource(emitter, node);
        }
        result.Seek(0L, SeekOrigin.Begin);
        return result;
    }

    public Node Deserialize(Stream stream,
                            ObjectId treeId,
                            DataPath path,
                            INodeSerializer.ItemLoader referenceResolver)
    {
        using var reader = new StreamReader(stream);
        using var parser = new NodeReferenceParser(reader, path, referenceResolver);
        var result = Deserializer.Deserialize<Node>(parser);

        var embeddedResource = ReadEmbeddedResource(stream);
        result = Model.UpdateBaseProperties(result, path, embeddedResource);

        var newType = Model.GetNewTypeIfDeprecated(result.GetType());
        if (newType is not null)
        {
            return Model.UpdateDeprecatedNode(result, newType);
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
