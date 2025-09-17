using System;

namespace GitObjectDb.Injection;

/// <summary>Instructs dependency injection that the constructor can be used for facatory delegates.</summary>
/// <remarks>Initializes a new instance of the <see cref="FactoryDelegateAttribute"/> class.</remarks>
/// <param name="delegateType">Type of the factory delegate.</param>
[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method)]
public sealed class FactoryDelegateAttribute(Type delegateType) : Attribute
{
    /// <summary>Gets the factory delegate type.</summary>
    public Type DelegateType { get; } = delegateType;
}
