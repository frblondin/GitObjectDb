using GitObjectDb.Api.Model;
using GitObjectDb.Model;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using System.Reflection;

namespace GitObjectDb.Api.OData.Model;

public static class EdmModelConverter
{
    public static IEdmModel ConvertToEdm(this IDataModel model, IEnumerable<Type> dtoTypes)
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

        UpdateTypeInheritance(model, builder);

        return builder.GetEdmModel();
    }

    private static void UpdateTypeInheritance(IDataModel model, ODataModelBuilder builder)
    {
        // foreach (var entityType in builder.NavigationSources.OfType<EntitySetConfiguration>())
        // {
        //     var attribute = type.GetCustomAttribute<DtoDescriptionAttribute>() ??
        //                     throw new NotSupportedException($"No {nameof(DtoDescriptionAttribute)} attribute defined for type '{type}'.");
        //
        //     var parent = builder.NavigationSources.OfType<EntitySetConfiguration>().FirstOrDefault(c => c.ClrType == entityType.ClrType.BaseType) ??
        //                  entityType.DerivesFrom(node);
        // }
    }

    private class EmptyAssemblyResolver : IAssemblyResolver
    {
        public IEnumerable<Assembly> Assemblies => Enumerable.Empty<Assembly>();
    }
}
