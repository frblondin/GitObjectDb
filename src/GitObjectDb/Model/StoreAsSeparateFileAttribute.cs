using System;

namespace GitObjectDb.Model;

/// <summary>Declares that a property should be stored in a separate blob, next to the main node.</summary>
[AttributeUsage(AttributeTargets.Property)]
public class StoreAsSeparateFileAttribute : Attribute
{
    /// <summary>Gets the file extension.</summary>
    public string Extension { get; init; } = "txt";
}