using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using System.Reflection;

namespace GitObjectDb.Api.OData.Model;

internal static class EdmModelConverter
{
    public static IEdmModel ConvertToEdm(this IEnumerable<Type> dtoTypes)
    {
        var builder = new ODataConventionModelBuilder(new EmptyAssemblyResolver());

        var node = builder.AddEntityType(typeof(NodeDto));
        node.HasKey(typeof(NodeDto).GetProperty(nameof(NodeDto.Id)));

        foreach (var type in dtoTypes)
        {
            var attribute = DtoDescriptionAttribute.Get(type);
            if (attribute.Type.IsAbstract)
            {
                continue;
            }

            var entityType = builder.AddEntityType(type);
            entityType.DerivesFrom(node);
            builder.AddEntitySet(attribute.EntitySetName, entityType);
        }

        return builder.GetEdmModel();
    }

    private sealed class EmptyAssemblyResolver : IAssemblyResolver
    {
        public IEnumerable<Assembly> Assemblies => Enumerable.Empty<Assembly>();
    }
}
