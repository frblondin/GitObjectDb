using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Reflection
{
    /// <summary>
    /// Selects the best constructor from a set of available constructors.
    /// </summary>
    internal interface IConstructorSelector
    {
        /// <summary>
        /// Selects the best constructor from the available constructors.
        /// </summary>
        /// <param name="type">The declaring type.</param>
        /// <param name="constructorBindings">Available constructors.</param>
        /// <returns>The best constructor.</returns>
        ConstructorParameterBinding SelectConstructorBinding(Type type, ConstructorParameterBinding[] constructorBindings);
    }
}
