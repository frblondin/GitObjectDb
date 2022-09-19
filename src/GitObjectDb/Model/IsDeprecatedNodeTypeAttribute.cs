using System;

namespace GitObjectDb.Model;

/// <summary>Instructs the deserializer that a newer type now exists.</summary>
[AttributeUsage(AttributeTargets.Class)]
public class IsDeprecatedNodeTypeAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="IsDeprecatedNodeTypeAttribute"/> class.</summary>
    /// <param name="newType">The type that should be returned by the deserializer.</param>
    public IsDeprecatedNodeTypeAttribute(Type newType)
    {
        NewType = newType;
    }

    /// <summary>Gets the type that should be returned by the deserializer.</summary>
    public Type NewType { get; }
}
