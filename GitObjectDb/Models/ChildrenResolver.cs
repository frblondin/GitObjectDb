using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Models
{
    /// <summary>
    /// Resolves children from the property name.
    /// </summary>
    /// <param name="parentType">Type of the parent.</param>
    /// <param name="propertyName">Name of the property.</param>
    /// <returns>An <see cref="ILazyChildren"/> instance containing the children.</returns>
    public delegate ILazyChildren ChildrenResolver(Type parentType, string propertyName);
}
