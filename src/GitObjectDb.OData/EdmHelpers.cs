using Microsoft.OData.Edm;

namespace GitObjectDb.OData
{
    internal static class EdmHelpers
    {
        /// <summary>
        /// Get element type reference if it's collection or return itself
        /// </summary>
        /// <param name="typeReference">The test type reference.</param>
        /// <returns>Element type or itself.</returns>
        // Token: 0x06000A4D RID: 2637 RVA: 0x000306D0 File Offset: 0x0002E8D0
        public static IEdmTypeReference? GetElementTypeOrSelf(this IEdmTypeReference typeReference)
        {
            if (ExtensionMethods.TypeKind(typeReference) == EdmTypeKind.Collection)
            {
                var edmCollectionTypeReference = EdmTypeSemantics.AsCollection(typeReference);
                return ExtensionMethods.ElementType(edmCollectionTypeReference);
            }
            return typeReference;
        }

        /// <summary>
        /// Get the elementType if it's collection or return itself's type
        /// </summary>
        /// <param name="edmTypeReference">The test type reference.</param>
        /// <returns>Element type or itself.</returns>
        // Token: 0x06000A4E RID: 2638 RVA: 0x000306FA File Offset: 0x0002E8FA
        public static IEdmType? GetElementType(this IEdmTypeReference edmTypeReference) =>
            edmTypeReference.GetElementTypeOrSelf()?.Definition;

        /// <summary>
        /// Converts the <see cref="T:Microsoft.OData.Edm.IEdmType" /> to <see cref="T:Microsoft.OData.Edm.IEdmCollectionType" />.
        /// </summary>
        /// <param name="edmType">The given Edm type.</param>
        /// <param name="isNullable">Nullable or not.</param>
        /// <returns>The collection type.</returns>
        // Token: 0x06000A4F RID: 2639 RVA: 0x0003070D File Offset: 0x0002E90D
        public static IEdmCollectionType ToCollection(this IEdmType edmType, bool isNullable) =>
            new EdmCollectionType(edmType.ToEdmTypeReference(isNullable));

        /// <summary>
        /// Converts an Edm Type to Edm type reference.
        /// </summary>
        /// <param name="edmType">The Edm type.</param>
        /// <param name="isNullable">Nullable value.</param>
        /// <returns>The Edm type reference.</returns>
        // Token: 0x06000A50 RID: 2640 RVA: 0x0003072C File Offset: 0x0002E92C
        public static IEdmTypeReference ToEdmTypeReference(this IEdmType edmType, bool isNullable) => edmType.TypeKind switch
        {
            EdmTypeKind.Primitive => EdmCoreModel.Instance.GetPrimitive(((IEdmPrimitiveType)edmType).PrimitiveKind, isNullable),
            EdmTypeKind.Entity => new EdmEntityTypeReference((IEdmEntityType)edmType, isNullable),
            EdmTypeKind.Complex => new EdmComplexTypeReference((IEdmComplexType)edmType, isNullable),
            EdmTypeKind.Collection => new EdmCollectionTypeReference((IEdmCollectionType)edmType),
            EdmTypeKind.EntityReference => new EdmEntityReferenceTypeReference((IEdmEntityReferenceType)edmType, isNullable),
            EdmTypeKind.Enum => new EdmEnumTypeReference((IEdmEnumType)edmType, isNullable),
            EdmTypeKind.TypeDefinition => new EdmTypeDefinitionReference((IEdmTypeDefinition)edmType, isNullable),
            EdmTypeKind.Path => new EdmPathTypeReference((IEdmPathType)edmType, isNullable),
            _ => throw new NotSupportedException(),
        };
    }
}
