using System;

namespace GitObjectDb.Injection;

/// <summary>Instructs dependency injection that the constructor can be used for facatory delegates.</summary>
[AttributeUsage(AttributeTargets.Constructor)]
public sealed class FactoryDelegateConstructorAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="FactoryDelegateConstructorAttribute"/> class.</summary>
    /// <param name="delegateType">Type of the factory delegate.</param>
    public FactoryDelegateConstructorAttribute(Type delegateType)
    {
        DelegateType = delegateType;
    }

    /// <summary>Gets the factory delegate type.</summary>
    public Type DelegateType { get; }
}
