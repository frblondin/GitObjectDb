using GitObjectDb.Api.OData.Model;
using GitObjectDb.Tests.Assets.Tools;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using static GitObjectDb.Api.OData.Tests.Model.BasicModel;

namespace GitObjectDb.Api.OData.Tests.Model;

public class DtoTypeEmitterTests
{
    [Test]
    [AutoDataCustomizations]
    public void SimpleNodeDtoGetsEmitted()
    {
        // Arrange
        var model = CreateDataModel(typeof(SimpleNode));

        // Act
        var sut = new DtoTypeEmitter(model);

        // Assert
        Assert.That(sut.TypeDescriptions, Has.Exactly(1).Items);
        Assert.Multiple(() =>
        {
            Assert.That(sut.TypeDescriptions[0].NodeType.Type, Is.SameAs(typeof(SimpleNode)));
            Assert.That(sut.TypeDescriptions[0].DtoType.Name, Is.EqualTo($"{nameof(SimpleNode)}DTO"));
            Assert.That(sut.TypeDescriptions[0].DtoType.GetProperties(), Has.Exactly(1).Matches<PropertyInfo>(
                p => p.Name == nameof(SimpleNode.Name)));
        });
    }

    [Test]
    [AutoDataCustomizations]
    public void SingleReferenceDtoGetsEmitted()
    {
        // Arrange
        var model = CreateDataModel(typeof(SimpleNode), typeof(SingleReferenceNode));

        // Act
        var sut = new DtoTypeEmitter(model);

        // Assert
        Assert.That(sut.TypeDescriptions, Has.Exactly(2).Items);
        Assert.Multiple(() =>
        {
            Assert.That(sut.TypeDescriptions[1].NodeType.Type, Is.SameAs(typeof(SingleReferenceNode)));
            Assert.That(sut.TypeDescriptions[1].DtoType.Name, Is.EqualTo($"{nameof(SingleReferenceNode)}DTO"));
            Assert.That(sut.TypeDescriptions[1].DtoType.GetProperties(), Has.Exactly(1).Matches<PropertyInfo>(
                p => p.Name == nameof(SingleReferenceNode.SingleReference) &&
                     p.PropertyType == sut.TypeDescriptions[0].DtoType));
        });
    }

    [Test]
    [AutoDataCustomizations]
    public void MultiReferenceDtoGetsEmitted()
    {
        // Arrange
        var model = CreateDataModel(typeof(SimpleNode), typeof(MultiReferenceNode));

        // Act
        var sut = new DtoTypeEmitter(model);

        // Assert
        Assert.That(sut.TypeDescriptions, Has.Exactly(2).Items);
        Assert.Multiple(() =>
        {
            Assert.That(sut.TypeDescriptions[1].NodeType.Type, Is.SameAs(typeof(MultiReferenceNode)));
            Assert.That(sut.TypeDescriptions[1].DtoType.Name, Is.EqualTo($"{nameof(MultiReferenceNode)}DTO"));
            Assert.That(sut.TypeDescriptions[1].DtoType.GetProperties(), Has.Exactly(1).Matches<PropertyInfo>(
                p => p.Name == nameof(MultiReferenceNode.MultiReference) &&
                     p.PropertyType == typeof(IEnumerable<>).MakeGenericType(sut.TypeDescriptions[0].DtoType)));
        });
    }

    [Test]
    [AutoDataCustomizations]
    public void NonApiBrowsableNodesGetSkipped()
    {
        // Arrange
        var model = CreateDataModel(typeof(NotBrowsableNode));

        // Act
        var sut = new DtoTypeEmitter(model);

        // Assert
        Assert.That(sut.TypeDescriptions, Has.Exactly(0).Items);
    }
}