using System.Reflection;

namespace GitObjectDb.Api;

/// <summary>Specifies whether a node type should be displayed in Api browsing.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ApiBrowsableAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref='ApiBrowsableAttribute'/> class.</summary>
    /// <param name="browsable">A value indicating whether a node type is browsable.</param>
    public ApiBrowsableAttribute(bool browsable)
    {
        Browsable = browsable;
    }

    /// <summary>Gets a value indicating whether a node type is browsable.</summary>
    public bool Browsable { get; }
}