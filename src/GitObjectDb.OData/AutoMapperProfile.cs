using AutoMapper;

namespace GitObjectDb.OData
{
    internal class AutoMapperProfile : Profile
    {
        internal const string ChildResolverName = "ChildResolver";

        public AutoMapperProfile(GeneratedTypesApplicationPart applicationPart)
        {
            var baseMapping = CreateMap<Node, NodeDTO>()
                .ForMember(
                    n => n.Path,
                    m => m.MapFrom((source, _) => source.Path?.FolderPath))
                .ForMember(
                    n => n.ChildResolver,
                    m => m.MapFrom(GetChildResolver));
            foreach (var (nodeType, dtoType, _) in applicationPart.TypeDescriptions)
            {
                CreateMap(nodeType.Type, dtoType);
                baseMapping.Include(nodeType.Type, dtoType);
            }

            Func<IEnumerable<NodeDTO>> GetChildResolver(Node source, NodeDTO destination, object? destinationMember, ResolutionContext context) => () =>
            {
                var children = context.GetChildResolver().Invoke(source);
                return context.Mapper.Map<IEnumerable<Node>, IEnumerable<NodeDTO>>(children);
            };
        }
    }

#pragma warning disable SA1402 // File may only contain a single type
    internal static class ResolutionContextExtensions
    {
        internal static Func<Node, IEnumerable<Node>> GetChildResolver(this ResolutionContext context) =>
            (Func<Node, IEnumerable<Node>>)context.Items[AutoMapperProfile.ChildResolverName];
    }
}
