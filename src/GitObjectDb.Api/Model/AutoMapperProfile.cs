using AutoMapper;
using Fasterflect;
using System.Reflection;

namespace GitObjectDb.Api.Model;

public class AutoMapperProfile : Profile
{
    internal const string ChildResolverName = "ChildResolver";

    public AutoMapperProfile(IEnumerable<TypeDescription> types)
    {
        var baseMapping = CreateMap<Node, NodeDto>()
            .ForMember(
                n => n.Path,
                m => m.MapFrom((source, _) => source.Path?.FolderPath))
            .ForMember(
                n => n.ChildResolver,
                m => m.MapFrom(GetChildResolver));
        AddTypes(types, baseMapping);

        Func<IEnumerable<NodeDto>> GetChildResolver(Node source,
                                                    NodeDto destination,
                                                    object? destinationMember,
                                                    ResolutionContext context) => () =>
                                                    {
                                                        var children = context.GetChildResolver().Invoke(source);
                                                        return context.Mapper.Map<IEnumerable<Node>, IEnumerable<NodeDto>>(children);
                                                    };
    }

    private void AddTypes(IEnumerable<TypeDescription> types, IMappingExpression<Node, NodeDto> baseMapping)
    {
        foreach (var description in types)
        {
            var mapping = CreateMap(description.NodeType.Type, description.DtoType)
                .ConstructUsing(src => Reflect.Constructor(description.DtoType, typeof(Node)).Invoke(src));

            MapReferenceProperties(description, mapping);

            baseMapping.Include(description.NodeType.Type, description.DtoType);
        }
    }

    /// <summary>Maps to other nodes must also be mapped to dto types.</summary>
    private static void MapReferenceProperties(TypeDescription description, IMappingExpression mapping)
    {
        foreach (var property in description.DtoType.GetProperties())
        {
            if (property.PropertyType.IsAssignableTo(typeof(NodeDto)))
            {
                MapSingleReference(description, mapping, property);
            }
            else if (property.IsEnumerable(t => t.IsAssignableTo(typeof(NodeDto)), out var dtoType))
            {
                MapMultiReference(description, mapping, property, dtoType!);
            }
        }
    }

    private static void MapSingleReference(TypeDescription description,
                                           IMappingExpression mapping,
                                           PropertyInfo property)
    {
        var sourceProperty = description.NodeType.Type.GetProperty(property.Name)!;
        var sourceGetter = Reflect.PropertyGetter(sourceProperty);
        mapping.ForMember(property.Name, c => c.MapFrom((original, _, _, context) =>
            context.Mapper.Map(sourceGetter(original),
                               sourceProperty.PropertyType,
                               property.PropertyType)));
    }

    private static void MapMultiReference(TypeDescription description,
                                          IMappingExpression mapping,
                                          PropertyInfo property,
                                          Type dtoType)
    {
        var sourceProperty = description.NodeType.Type.GetProperty(property.Name)!;
        if (!sourceProperty.IsEnumerable(t => t.IsAssignableTo(typeof(Node)), out var nodeType))
        {
            throw new NotSupportedException("Could not find node type of multi reference.");
        }
        var sourceGetter = Reflect.PropertyGetter(sourceProperty);
        mapping.ForMember(property.Name, c => c.MapFrom((original, _, _, context) =>
            context.Mapper.Map(sourceGetter(original),
                               typeof(IEnumerable<>).MakeGenericType(nodeType!),
                               typeof(IEnumerable<>).MakeGenericType(dtoType))));
    }
}

#pragma warning disable SA1402 // File may only contain a single type
public static class ResolutionContextExtensions
{
    internal static Func<Node, IEnumerable<Node>> GetChildResolver(this ResolutionContext context) =>
        (Func<Node, IEnumerable<Node>>)context.Items[AutoMapperProfile.ChildResolverName];
}
