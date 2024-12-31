using AutoFixture;
using AutoFixture.NUnit3;
using System;

namespace GitObjectDb.Tests.Assets.Tools;

[AttributeUsage(AttributeTargets.Method)]
public sealed class AutoDataCustomizationsAttribute : AutoDataAttribute
{
    public AutoDataCustomizationsAttribute(params Type[] customizationTypes)
        : base(() => CreateFixture(customizationTypes))
    {
        TestMethodBuilder = new DeferredArgumentTestMethodBuilder();
    }

    internal static IFixture CreateFixture(params Type[] customizationTypes)
    {
        var result = new Fixture();
        foreach (var type in customizationTypes)
        {
            var customization = (ICustomization)Activator.CreateInstance(type);
            result.Customize(customization);
        }
        return result;
    }
}
