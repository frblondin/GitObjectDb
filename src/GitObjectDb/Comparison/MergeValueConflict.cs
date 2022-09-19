using System;
using System.Reflection;

namespace GitObjectDb.Comparison;

/// <summary>Represents a conflict between two versions and a common ancestor.</summary>
public sealed class MergeValueConflict
{
    private readonly Action<object> _resolveCallback;

    internal MergeValueConflict(PropertyInfo property, object? ancestorValue, object? ourValue, object? theirValue, Action<object> resolveCallback)
    {
        Property = property;
        AncestorValue = ancestorValue;
        OurValue = ourValue;
        TheirValue = theirValue;
        _resolveCallback = resolveCallback;
    }

    /// <summary>Gets the value property.</summary>
    public PropertyInfo Property { get; }

    /// <summary>Gets the ancestor value.</summary>
    public object? AncestorValue { get; }

    /// <summary>Gets our value.</summary>
    public object? OurValue { get; }

    /// <summary>Gets their value.</summary>
    public object? TheirValue { get; }

    /// <summary>Gets the resolved value.</summary>
    public object? ResolvedValue { get; private set; }

    /// <summary>Gets a value indicating whether this instance is resolved.</summary>
    public bool IsResolved { get; private set; }

    /// <summary>Resolves the conflict with the specified value.</summary>
    /// <param name="value">The value.</param>
    public void Resolve(object value)
    {
        IsResolved = true;
        ResolvedValue = value;
        _resolveCallback(value);
    }
}
