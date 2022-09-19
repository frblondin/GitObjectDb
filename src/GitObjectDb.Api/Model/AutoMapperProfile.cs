using AutoMapper;
using Fasterflect;

namespace GitObjectDb.Api.Model;

public class AutoMapperProfile : Profile
{
    public const string ChildResolverName = "ChildResolver";

    public AutoMapperProfile(IEnumerable<TypeDescription> types)
    {
        var baseMapping = CreateMap<Node, NodeDTO>()
            .ForMember(
                n => n.Path,
                m => m.MapFrom((source, _) => source.Path?.FolderPath))
            .ForMember(
                n => n.ChildResolver,
                m => m.MapFrom(GetChildResolver));
        foreach (var description in types)
        {
            CreateMap(description.NodeType.Type, description.DtoType)
                .ConstructUsing(src => Reflect.Constructor(description.DtoType, typeof(Node)).Invoke(src));
            baseMapping.Include(description.NodeType.Type, description.DtoType);
        }

        Func<IEnumerable<NodeDTO>> GetChildResolver(Node source, NodeDTO destination, object? destinationMember, ResolutionContext context) => () =>
        {
            var children = context.GetChildResolver().Invoke(source);
            return context.Mapper.Map<IEnumerable<Node>, IEnumerable<NodeDTO>>(children);
        };
    }
}

#pragma warning disable SA1402 // File may only contain a single type
public static class ResolutionContextExtensions
{
    internal static Func<Node, IEnumerable<Node>> GetChildResolver(this ResolutionContext context) =>
        (Func<Node, IEnumerable<Node>>)context.Items[AutoMapperProfile.ChildResolverName];
}
