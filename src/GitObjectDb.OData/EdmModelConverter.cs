using GitObjectDb.Model;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using System.Reflection;

namespace GitObjectDb.OData
{
    public static class EdmModelConverter
    {
        public static IEdmModel ConvertToEdm(this IDataModel model, IEnumerable<Type> dtoTypes)
        {
            var builder = new ODataConventionModelBuilder();

            var node = builder.AddEntityType(typeof(NodeDTO));
            node.HasKey(typeof(NodeDTO).GetProperty(nameof(NodeDTO.Id)));

            foreach (var type in dtoTypes)
            {
                var attribute = type.GetCustomAttribute<DtoDescriptionAttribute>() ??
                    throw new NotSupportedException($"No {nameof(DtoDescriptionAttribute)} attribute defined for type '{type}'.");

                var entityType = builder.AddEntityType(type).DerivesFrom(node);
                builder.AddEntitySet(attribute.EntitySetName, entityType);
            }

            return builder.GetEdmModel();
        }
    }
}