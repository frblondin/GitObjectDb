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
    public void BindFromNestedType()
    {
        // Act
        var result = TypeHelper.BindToName(typeof(NestedType));

        // Assert
        Assert.That(result, Is.EqualTo(typeof(NestedType).FullName));
    }

    private class NestedType
    {
    }
}
