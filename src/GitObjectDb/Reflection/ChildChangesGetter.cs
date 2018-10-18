using GitObjectDb.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Reflection
{
    /// <summary>
    /// Represents a method that returns the nodes that must be modified in a child collection.
    /// </summary>
    /// <param name="instance">The instance.</param>
    /// <param name="childProperty">Child property info.</param>
    /// <returns>The child additions and deletions.</returns>
    public delegate (IEnumerable<IModelObject> Additions, IEnumerable<IModelObject> Deletions) ChildChangesGetter(IModelObject instance, ChildPropertyInfo childProperty);
}
