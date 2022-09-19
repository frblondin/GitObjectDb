using System;

namespace GitObjectDb.Model;

/// <summary>Declares that a property is searchable.</summary>
[AttributeUsage(AttributeTargets.Property)]
public class IsSearchableAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="IsSearchableAttribute"/> class.</summary>
    /// <param name="searchable">A value indicating whether a node type is searchable.</param>
    public IsSearchableAttribute(bool searchable = true)
    {
        Searchable = searchable;
    }

    /// <summary>Gets a value indicating whether a property is searchable.</summary>
    public bool Searchable { get; }
}
