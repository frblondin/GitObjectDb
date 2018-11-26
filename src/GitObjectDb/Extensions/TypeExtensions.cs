using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace System
{
    /// <summary>
    /// A set of methods for instances of <see cref="Type"/>.
    /// </summary>
    internal static class TypeExtensions
    {
        private const string DiscriminitedUnionGeneratorName = "DiscriminitedUnion";

        /// <summary>
        /// Determines whether the type is a discriminated union.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        ///   <c>true</c> if the type is a discriminated union; otherwise, <c>false</c>.
        /// </returns>
        internal static bool IsDiscriminatedUnion(this Type type) =>
            type.GetCustomAttribute<GeneratedCodeAttribute>(true)?.Tool?.StartsWith(DiscriminitedUnionGeneratorName, StringComparison.OrdinalIgnoreCase) ?? false;
    }
}
