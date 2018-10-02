using System;

namespace GitObjectDb.Reflection
{
    /// <summary>
    /// Provides information about model objects.
    /// </summary>
    public interface IModelDataAccessorProvider
    {
#pragma warning disable CA1716 // Identifiers should not match keywords
        /// <summary>
        /// Gets the <see cref="IModelDataAccessor"/> instance for the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
                              /// <returns>The <see cref="IModelDataAccessor"/> instance.</returns>
        IModelDataAccessor Get(Type type);
#pragma warning restore CA1716 // Identifiers should not match keywords
    }
}
