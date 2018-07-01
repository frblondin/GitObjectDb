using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Attributes
{
    /// <summary>
    /// Instructs the GitToObjectDb engine that a property can be modified.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ModifiableAttribute : Attribute
    {
    }
}
