using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Model;

/// <summary>Declares that a node type contains children of type <see cref="Type"/>.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class HasChildAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="HasChildAttribute"/> class.</summary>
    /// <param name="childType"><see cref="Type"/> of the childen.</param>
    public HasChildAttribute(Type childType)
    {
        ChildType = childType;
    }

    /// <summary>Gets the <see cref="Type"/> of the childen.</summary>
    public Type ChildType { get; }
}
