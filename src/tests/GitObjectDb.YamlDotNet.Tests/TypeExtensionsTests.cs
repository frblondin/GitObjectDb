using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace GitObjectDb.YamlDotNet.Tests;

public class TypeExtensionsTests
{

    [Test]
    [TestCase(typeof(int), "int")]
    [TestCase(typeof(int?), "int?")]
    [TestCase(typeof(string), "string")]
    [TestCase(typeof(byte[]), "Array(byte)")]
    [TestCase(typeof(byte[][]), "Array(Array(byte))")]
    [TestCase(typeof(string[,]), "Array(string)")]
    [TestCase(typeof(List<int>), "System.Collections.Generic.List(int)")]
    [TestCase(typeof(List<>), "System.Collections.Generic.List()")]
    [TestCase(typeof(Dictionary<int, string>), "System.Collections.Generic.Dictionary(int,string)")]
    [TestCase(typeof(Dictionary<int, Dictionary<string, char>>), "System.Collections.Generic.Dictionary(int,System.Collections.Generic.Dictionary(string,char))")]
    [TestCase(typeof(MyStruct), "GitObjectDb.YamlDotNet.Tests.TypeExtensionsTests.MyStruct")]
    [TestCase(typeof(Nullable<MyStruct>), "GitObjectDb.YamlDotNet.Tests.TypeExtensionsTests.MyStruct?")]
    [TestCase(typeof(MyStruct?), "GitObjectDb.YamlDotNet.Tests.TypeExtensionsTests.MyStruct?")]
    [TestCase(typeof(MyClass), "GitObjectDb.YamlDotNet.Tests.TypeExtensionsTests.MyClass")]
    [TestCase(typeof(MyGenericClass<MyClass>), "GitObjectDb.YamlDotNet.Tests.TypeExtensionsTests.MyGenericClass(GitObjectDb.YamlDotNet.Tests.TypeExtensionsTests.MyClass)")]
    public void GetYamlName(Type type, string typename)
    {
        Assert.That(type.GetYamlName(), Is.EqualTo(typename));
    }

    internal struct MyStruct
    {
    }

    internal class MyClass
    {
    }

    internal class MyGenericClass<T>
    {
    }
}
