using AutoMapper;
using Fasterflect;

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
            CreateMap(description.NodeType.Type, description.DtoType)
                .ConstructUsing(src => Reflect.Constructor(description.DtoType, typeof(Node)).Invoke(src));
            baseMapping.Include(description.NodeType.Type, description.DtoType);
        }
    }
}

#pragma warning disable SA1402 // File may only contain a single type
public static class ResolutionContextExtensions
{
    internal static Func<Node, IEnumerable<Node>> GetChildResolver(this ResolutionContext context) =>
        (Func<Node, IEnumerable<Node>>)context.Items[AutoMapperProfile.ChildResolverName];
}
