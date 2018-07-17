using GitObjectDb.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Reflection
{
    /// <summary>
    /// Represents a method that processes an argument.
    /// </summary>
    /// <param name="instance">The instance.</param>
    /// <param name="propertyName">Name of the property.</param>
    /// <param name="type">The argument type.</param>
    /// <param name="fallback">The fallback.</param>
    /// <returns>The new argument value.</returns>
    public delegate object ProcessArgument(IMetadataObject instance, string propertyName, Type type, object fallback);
}
