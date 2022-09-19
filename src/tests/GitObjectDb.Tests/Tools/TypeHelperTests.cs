using GitObjectDb.Tests.Assets.Tools;
using GitObjectDb.Tools;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;

namespace GitObjectDb.Tests.Tools;

public class TypeHelperTests
{
    [Test]
    [AutoDataCustomizations]
    public void FindAssemblyDelimiterIndex()
    {
        // Act
        var assemblyQualifiedName = typeof(IEnumerable<IEnumerable<string>>).AssemblyQualifiedName;
        var index = TypeHelper.GetAssemblyDelimiterIndex(assemblyQualifiedName);

        // Arrange
        var assemblyName = typeof(IEnumerable<>).Assembly.GetName().Name;
        Assert.That(index, Is.EqualTo(assemblyQualifiedName.LastIndexOf(assemblyName, System.StringComparison.OrdinalIgnoreCase) - 2));
    }

    [Test]
    [AutoDataCustomizations]
    public void BindFromNestedType()
    {
        // Act
        var result = TypeHelper.BindToName(typeof(NestedType));

        // Assert
        Assert.That(result, Is.EqualTo($"{typeof(NestedType).FullName}, {Assembly.GetExecutingAssembly().FullName}"));
    }

    [Test]
    [AutoDataCustomizations]
    public void BindToNestedType()
    {
        // Act
        var result = TypeHelper.BindToType($"{typeof(NestedType).FullName}, {Assembly.GetExecutingAssembly().FullName}");

        // Assert
        Assert.That(result, Is.EqualTo(typeof(NestedType)));
    }

    private class NestedType
    {
    }
}
